using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private string[] slotTexts = new string[20];
    private Plugin plugin;
    private bool dirty;

    public MainWindow(Plugin plugin) : base("Glamour Names")
    {
        Flags = ImGuiWindowFlags.NoResize;

        Size = new Vector2(200, 560);
        SizeCondition = ImGuiCond.Always;
        this.plugin = plugin;

        configuration = plugin.Configuration;

        // Set the displayed values on the window to the values that are saved
        for (var i = 0; i < slotTexts.Length; i++)
        {
            slotTexts[i] = configuration.SlotNames[i];
        }
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    public override void Draw()
    {
        for (var i = 0; i < 20; i++)
        {
            ImGui.InputText($"Plate {i + 1}", ref slotTexts[i], 5);

            configuration.SlotNames[i] = slotTexts[i];
            dirty = true;
            plugin.GuiText((uint)(i + 6));
            
        }

        if (dirty && !ImGui.IsAnyItemActive())
        {
            configuration.Save();
            dirty = false;
        }
    }

    public override void OnClose()
    {
        if (dirty)
        {
            configuration.Save();
            dirty = false;
        }
    }
}
