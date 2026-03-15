using Aite.Config.Entities;
using Aite.Config.Manager;
using Aite.Config.Utils;
using Aite.Core.Manager;
using Aite.Core.Message;
using Aite.Core.Utils.ViewLogger;
using Serilog;
using Serilog.Events;
using WPFLauncherApi.Http;
using WPFLauncherApi.Protocol;

namespace Aite.Core.Utils;

public static class InitProgram {
    public static void LogoInit()
    {
        // 清空框架信息
        Console.Clear();

        // 配置 Serilog 日志记录
        var logger = new Logger();
        logger.MinimumLevel.Information();
        logger.SetColor(LogEventLevel.Information, ConsoleColor.Yellow);
        logger.SetColor(LogEventLevel.Warning, ConsoleColor.DarkYellow);
        logger.SetColor(LogEventLevel.Error, ConsoleColor.Red);
        logger.SetColor(LogEventLevel.Fatal, ConsoleColor.DarkRed);
        Log.Logger = logger.CreateLogger();
    }

    public static void NelInit(string[] args, Action logInit)
    {
        // 日志初始化
        logInit.Invoke();
        // 释放 7z.exe
        Extract7zExe();
        
        // 检查更新
        //UpdateTools.CheckUpdate(args).Wait();
    }
    
    private static void Extract7zExe()
    {
        try
        {
            var assembly = typeof(InitProgram).Assembly;
            
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.FirstOrDefault(n => n.Contains("7z.exe"));
            
            if (string.IsNullOrEmpty(resourceName))
            {
                Log.Warning("未找到 7z.exe 嵌入资源，可用资源: {Resources}", string.Join(", ", resourceNames));
                return;
            }
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Warning("未找到 7z.exe 嵌入资源: {ResourceName}", resourceName);
                return;
            }
            
            var exePath = Path.Combine(PathUtil.CachePath, "7z.exe");
            
            if (File.Exists(exePath))
            {
                Log.Information("7z.exe 已存在，跳过释放");
                return;
            }
            
            using var fileStream = File.Create(exePath);
            stream.CopyTo(fileStream);
            
            Log.Information("成功释放 7z.exe 到: {Path}", exePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "释放 7z.exe 失败");
        }
    }

    public static void NelInit1()
    {
        var crcSaltResult = crcSaltInit().Result;
        if (!crcSaltResult) {
            Log.Warning("CRC Salt 获取失败，部分功能可能受限");
        }

        // 插件初始化
        // 避免插件过早的加载，因为这是没必要的
        // await InitializeSystemComponentsAsync();
        
        // 默认登录
        AccountMessage.GetAccountList();

        // 插件管理器初始化
        PluginMessage.Initialize();

    }

    private static async Task<bool> crcSaltInit()
    {
        // 检查X19.CrcSalt是否已经设置
        if (!string.IsNullOrEmpty(WPFLauncherApi.Protocol.X19.CrcSalt))
        {
            Log.Information("CRC Salt 计算完成");
            return true; // 成功获取，返回 true
        }

        return false; // 获取失败，返回 false
    }
}