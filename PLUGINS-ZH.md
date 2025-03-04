<h1 align="center">
插件系统
</h1>
<p align="center">
TerraAngel 支持通过插件形式进行扩展
</p>
<br>

# 快速入门

## 插件加载

TerraAngel 会自动识别 `Plugins` 文件夹中所有以 `.TAPlugin.dll` 结尾的 .NET 类库作为插件。

当插件被加载时，TerraAngel 会在插件程序集中查找继承自 `TerraAngel.PluginAPI` 中定义的 `Plugin` 类的具体实现。

## 示例插件

创建一个针对 .NET 7.0 的 .NET 类库项目，名称以 .TAPlugin 结尾（例如 Example.TAPlugin）

可通过命令行创建：`dotnet new classlib --name Example.TAPlugin --framework net7.0`

```cs
using TerraAngel;
using TerraAngel.Plugin;

namespace Example.TAPlugin;

public class ExamplePlugin : Plugin
{
    public override string Name => "示例插件";

    public ExamplePlugin(string path) : base(path)
    {

    }

    // 插件加载时调用一次
    public override void Load()
    {

    }

    // 插件卸载时调用一次
    public override void Unload()
    {

    }

    // 插件加载期间每帧调用
    public override void Update()
    {

    }
}
```

需要添加对以下程序集的引用：
- `TerraAngelPluginAPI.dll`
- `Terraria.dll`

建议同时引用：
 - `ReLogic.dll`
 - `ImGui.NET.dll`
 - `TNA.dll`

这些文件位于 TerraAngel 的构建目录中（`TerraAngel/Terraria/bin/Release/net7.0/`）

安装插件时，将插件 DLL 复制到 TerraAngel 的插件目录即可。

# 其他示例

## 添加控制台命令

```cs
using TerraAngel;
using TerraAngel.Plugin;
using Terraria.DataStructures;

namespace Console.TAPlugin;

public class ConsoleExamplePlugin : Plugin
{
    public override string Name => "控制台示例插件";

    public ConsoleExample(string path) : base(path)
    {

    }

    // 插件加载时调用
    public override void Load()
    {
        // 向控制台添加命令
        ClientLoader.Console.AddCommand("kill_player",
            (x) =>
            {
                Main.LocalPlayer.KillMe(PlayerDeathReason.ByPlayer(Main.myPlayer), 1, 0);
            },
            "杀死玩家");
    }

    // 插件卸载时调用
    public override void Unload()
    {

    }

    // 每帧更新
    public override void Update()
    {
        
    }
}
```