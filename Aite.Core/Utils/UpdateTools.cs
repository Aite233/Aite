using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Serilog;
using WPFLauncherApi.Http;

namespace Aite.Core.Utils;

public static class UpdateTools {
    // 检查更新
    public static async Task CheckUpdate(string[] args)
    {
        await CheckUpdate("static", "Resource");
        await CheckUpdate("static." + PublicProgram.Mode, "Resource");
        await CheckUpdate("static." + PublicProgram.Mode + "." + PublicProgram.Arch, "Resource", false, false);
    }

    /**
     * 检查更新
     * @param name 名称
     * @param safe 是否安全模式
     */
    public static async Task<int> CheckUpdate(string mode, string name = "", bool safe = false, bool failureLog = true)
    {
        var jsonObj =
            await X19Extensions.Aite.Api<JsonObject>(
                $"/api/fantnel/update/get?mode={mode}");
        if (jsonObj == null) {
            if (!failureLog) return -1;
            Log.Error("{name}: {mode}", name, mode);
            Log.Error("检查更新失败, 建议更新至最新版本!");
            return -1;
        }

        var data = jsonObj["data"];
        if (data == null) {
            if (!failureLog) return -1;
            Log.Error("{name}: {mode}", name, mode);
            Log.Error("检查更新失败, 建议更新至最新版本!");
            return -1;
        }

        var array = data.AsArray();
        await ThreadUpdateTools.CheckUpdate(array, name, safe);
        return array.Count;
    }
}