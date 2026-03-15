﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Codexus.Game.Launcher.Utils;
using Aite.Config.Entities;
using Aite.Config.Utils.CodeTools;
using Aite.Core.Entities.Aite;
using Aite.Core.Entities.Plugin;
using Aite.Core.Manager;
using WPFLauncherApi.Http;

namespace Aite.Core.Message;

public static class PlugInstoreMessage {
    // 插件列表 - 缓存
    private static readonly List<EntityComponents> PluginList = [];

    public static async Task<EntityComponents[]> GetPluginList(int offset = 0, int limit = 10)
    {
        // 先检查缓存是否足够
        bool needFetch = false;
        lock (LockManager.PluginListLock) {
            // 计算需要的大小
            var size = offset + (limit - 10);
            needFetch = PluginList.Count < size;
        }
        
        // 如果需要获取更多数据，先获取
        if (needFetch) {
            var size = offset + (limit - 10);
            await GetPluginList(0, size);
        }
        
        // 再次检查缓存并返回数据
        lock (LockManager.PluginListLock) {
            // 分页
            var size = (offset == 0 ? 1 : offset) * limit;
            if (PluginList.Count >= size)
                return PluginList.Skip(size - limit).Take(limit).ToArray();
        }

        // 没有 就从 插件商店 获取
        var plugins =
            await X19Extensions.Aite.Api<EntityResponse<EntityComponents[]>>(
                $"/api/fantnel/plugin/get?offset={offset}&limit={limit}");
        if (plugins?.Data == null) throw new ErrorCodeException(ErrorCode.FormatError);
        AddServerList(plugins.Data);
        return plugins.Data;
    }

    // 插件列表 - 添加
    private static void AddServerList(EntityComponents[] entities)
    {
        foreach (var entity in entities)
            AddServerList(entity);
    }

    // 插件列表 - 添加
    private static void AddServerList(EntityComponents entity)
    {
        lock (LockManager.PluginListLock) {
            // 插件列表 没有 就添加
            if (PluginList.All(plugin => plugin.Id != entity.Id))
                PluginList.Add(entity);
        }
    }

    public static async Task<EntityResponse<EntityPlugin>?> GetPluginDetail(string id)
    {
        return await X19Extensions.Aite.Api<EntityResponse<EntityPlugin>>($"/api/fantnel/plugin/get/by-id?id={id}");
    }

    private static async Task<EntityResponse<EntityPluginDownResponse>?> GetDownloadInfoUrl(string id)
    {
        return await X19Extensions.Aite
            .Api<EntityResponse<EntityPluginDownResponse>>($"/api/fantnel/plugin/get/download?id={id}");
    }

    private static string GetDownloadUrl(string id)
    {
        return $"http://110.42.70.32:13423/api/fantnel/plugin/download?id={id}";
    }

    /**
     * 插件列表 - 自动更新检测
     */
    public static async Task AutoUpdateCheck()
    {
        // 清理相同ID的插件
        PluginMessage.CleanSameIdPlugin();

        var plugins = PluginMessage.GetPluginList();
        foreach (var plugin in plugins) {
            var downloadInfo = await GetDownloadInfoUrl(plugin.Id);
            if (downloadInfo?.Data == null || downloadInfo.Code != 1) {
                continue;
            }
            
            // 检测 插件 是否需要更新
            if (NoEqualsPlugin(downloadInfo.Data.FileHash, downloadInfo.Data.FileSize)) {
                lock (plugin.Id) {
                    PluginMessage.DeletePlugin(plugin.Id);
                }
                await Download(plugin.Id);
            }

            // 依赖插件 为空 则 跳过，不检测依赖插件
            if (downloadInfo.Data?.Dependencies == null) {
                continue;
            }

            // 检测 依赖插件 是否需要更新
            foreach (var item in downloadInfo.Data.Dependencies) {
                if (!NoEqualsPlugin(item.FileHash, item.FileSize)) {
                    continue;
                }
                lock (plugin.Id) {
                    PluginMessage.DeletePlugin(plugin.Id);
                }
                await Download(item.Id);
            }
        }

        // 清理相同ID的插件
        PluginMessage.CleanSameIdPlugin();
    }

    // 插件列表 - 下载
    private static async Task Download(string id)
    {
        var detail = await GetPluginDetail(id);
        if (detail?.Data?.Name == null) throw new ErrorCodeException(ErrorCode.NotFound);
        // 下载插件 保存路径
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        // 自动插件 插件 文件夹
        Directory.CreateDirectory(path);
        // 自动插件 插件 文件名
        path = Path.Combine(path, detail.Data.Name + " [" + detail.Data.Version + "].dll");
        await DownloadUtil.DownloadAsync(GetDownloadUrl(id), path, detail.Data.Name + " [" + detail.Data.Version + "]");
    }

    /**
     * 检测 插件/依赖 (存在且[md5]匹配)
     * @param fileMd5 文件md5，为空则不校验
     * @param fileSize 文件大小，为空则不校验
     * @return 不匹配:true，匹配:false
     */
    private static bool NoEqualsPlugin(string? fileMd5, long? fileSize)
    {
        // 获取 插件目录数组 和 md5数组
        var filesPath = PluginMessage.GetPluginDirectoryAndMd5List();
        for (var i = 0; i < filesPath.Item2.Length; i++) {
            // MD5 不匹配 则跳过
            if (fileMd5 is { Length: > 31 } && !filesPath.Item2[i].Equals(fileMd5)) continue;
            // 文件大小 匹配 则返回
            var file = new FileInfo(filesPath.Item1[i]);
            if (fileSize != null && fileSize == file.Length) return false;
        }

        return true;
    }

    /**
     * 检测 插件是否已安装
     * @param id 插件ID
     * @return 已安装:true，未安装:false
     */
    private static bool IsPluginInstalled(string id)
    {
        return PluginMessage.IsPluginExist(id);
    }

    public static async Task Install(string id)
    {
        lock (id) {
            var downloadInfo = GetDownloadInfoUrl(id).Result;
            if (downloadInfo?.Data == null || downloadInfo.Code != 1)
                throw new ErrorCodeException(ErrorCode.Failure, downloadInfo);

            // 无论插件是否已安装，都执行下载操作（用于更新）
            Download(id).Wait();
            // 清理相同ID的插件，确保只保留最新版本
            PluginMessage.CleanSameIdPlugin();

            // 依赖插件 为空 则 直接返回成功
            if (downloadInfo.Data?.Dependencies == null) return;

            // 检测 依赖插件是否已安装
            foreach (var item in downloadInfo.Data.Dependencies)
                if (!IsPluginInstalled(item.Id))
                {
                    Download(item.Id).Wait();
                    // 清理相同ID的插件，确保只保留最新版本
                    PluginMessage.CleanSameIdPlugin();
                }
        }
    }
}