using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SamplePlugin.Windows;
using System.IO;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonItemDetailCompare;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    private const string CommandName = "/glamnames";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("GlamNames");
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        PluginInterface.UiBuilder.Draw += GlamToggleButton;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"{PluginInterface.Manifest.Name} loaded");

        AddonLifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "MiragePrismMiragePlate", OnPostRefresh);

    }

    public void OnPostRefresh(AddonEvent type, AddonArgs args)
    {
        for (var i = 6; i < 26; i++)
        {
            GuiText((uint)i);
        }
    }

    public unsafe void GuiText(uint slot)
    {
        var addon = GameGui.GetAddonByName("MiragePrismMiragePlate");
        if (addon.IsNull) return;

        var glamGui = (AtkUnitBase*)addon.Address;
        if (!glamGui->IsVisible) return;

        AtkResNode* glamSlot1 = glamGui->GetNodeById(slot);
        if (glamSlot1 == null) { return; }

        var component = ((AtkComponentNode*)glamSlot1)->Component;
        if (component == null) return;

        var node = component->UldManager.SearchNodeById(2);
        if (node != null && (NodeType)node->Type == NodeType.Text)
        {
            ((AtkTextNode*)node)->SetText(Configuration.SlotNames[slot - 6]);
        }
    }

    public unsafe void GlamToggleButton()
    {
        var addon = GameGui.GetAddonByName("MiragePrismMiragePlate");
        if (addon.IsNull) return;

        var glamGui = (AtkUnitBase*)addon.Address;
        if (glamGui == null || !glamGui->IsVisible) return;

        var root = glamGui->RootNode;
        if (root == null) return;

        var width = root->Width * glamGui->Scale;
        ImGui.SetNextWindowPos(new Vector2(glamGui->X + width + 5, glamGui->Y + 10), ImGuiCond.Always);
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("GlamToggleButton", flags))
        {
            if (ImGui.Button("Edit"))
            {
                MainWindow.Toggle();
            }
        }
        ImGui.End();
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw -= GlamToggleButton;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "MiragePrismMiragePlate", OnPostRefresh);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    public void ToggleMainUi() => MainWindow.Toggle();
}
