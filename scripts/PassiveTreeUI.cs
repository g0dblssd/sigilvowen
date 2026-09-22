using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

public partial class PassiveTreeUI : CanvasLayer
{
    private PlayerProgression? _progression;
    private Control? _panel;
    private Label? _summary;
    private OptionButton? _classOption;
    private ScrollContainer? _treeScroll;
    private PassiveTreeCanvas? _treeCanvas;

    public void Setup(PlayerProgression progression)
    {
        _progression = progression;
    }

    public override void _Ready()
    {
        Layer = 20;
        if (_progression == null)
        {
            return;
        }
        _progression.Changed += RefreshHeader;

        var panel = new PanelContainer
        {
            AnchorLeft = 0.04f,
            AnchorTop = 0.04f,
            AnchorRight = 0.96f,
            AnchorBottom = 0.96f,
            Visible = false,
        };
        _panel = panel;
        AddChild(panel);

        var layout = new VBoxContainer();
        panel.AddChild(layout);
        var header = new HBoxContainer();
        layout.AddChild(header);

        var title = new Label { Text = "SIGIL CONSTELLATION  //  361 PASSIVE NODES" };
        title.AddThemeFontSizeOverride("font_size", 19);
        title.Modulate = new Color(0.55f, 0.85f, 1f);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(title);

        _classOption = new OptionButton();
        foreach (HeroClass heroClass in System.Enum.GetValues<HeroClass>())
        {
            _classOption.AddItem(heroClass.ToString().ToUpperInvariant(), (int)heroClass);
        }
        _classOption.Selected = (int)_progression.HeroClass;
        _classOption.ItemSelected += OnClassSelected;
        header.AddChild(_classOption);

        var close = new Button { Text = "CLOSE (P)" };
        close.Pressed += Toggle;
        header.AddChild(close);

        _summary = new Label();
        layout.AddChild(_summary);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _treeScroll = scroll;
        layout.AddChild(scroll);
        scroll.AddChild(new Label
        {
            Text = "Open the constellation to weave its 361 passive nodes.",
            Position = new Vector2(24f, 24f),
            Modulate = new Color(0.6f, 0.72f, 0.9f),
        });
        RefreshHeader();
    }

    public override void _ExitTree()
    {
        if (_progression != null)
        {
            _progression.Changed -= RefreshHeader;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.P)
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Toggle()
    {
        if (_panel != null)
        {
            _panel.Visible = !_panel.Visible;
            if (_panel.Visible)
            {
                EnsureTreeBuilt();
            }
        }
    }

    private void EnsureTreeBuilt()
    {
        if (_treeCanvas != null || _treeScroll == null || _progression == null)
        {
            return;
        }
        foreach (Node child in _treeScroll.GetChildren())
        {
            child.QueueFree();
        }
        _treeCanvas = new PassiveTreeCanvas();
        _treeCanvas.Setup(_progression);
        _treeScroll.AddChild(_treeCanvas);
        _treeScroll.ScrollHorizontal = 560;
        _treeScroll.ScrollVertical = 560;
    }

    private void OnClassSelected(long selected)
    {
        _progression?.SetClass((HeroClass)selected);
        RefreshHeader();
    }

    private void RefreshHeader()
    {
        if (_progression == null || _summary == null)
        {
            return;
        }
        _summary.Text = $"Class: {_progression.HeroClass}  |  Free points: {_progression.PassivePoints}  |  Power {_progression.PassivePowerRanks}  Vitality {_progression.PassiveVitalityRanks}  Haste {_progression.PassiveHasteRanks}  Mana {_progression.PassiveManaRanks}";
        if (_classOption != null)
        {
            _classOption.Disabled = _progression.Level > 1 || _progression.Experience > 0;
            _classOption.Selected = (int)_progression.HeroClass;
        }
        _treeCanvas?.RefreshNodes();
    }
}

public partial class PassiveTreeCanvas : Control
{
    private sealed class NodeData
    {
        public int Id;
        public int ParentId;
        public PassiveStat Stat;
        public int Ranks;
        public Vector2 Center;
        public Button? Button;
    }

    private PlayerProgression _progression = null!;
    private readonly List<NodeData> _nodes = new();
    private readonly Dictionary<int, NodeData> _byId = new();
    private static readonly Color[] StatColors =
    {
        new(1f, 0.35f, 0.18f),
        new(0.3f, 0.9f, 0.42f),
        new(0.35f, 0.72f, 1f),
        new(0.72f, 0.38f, 1f),
    };

    public void Setup(PlayerProgression progression)
    {
        _progression = progression;
        CustomMinimumSize = new Vector2(2000f, 2000f);
        MouseFilter = MouseFilterEnum.Pass;
    }

    public override void _Ready()
    {
        BuildTree();
        RefreshNodes();
    }

    public override void _Draw()
    {
        foreach (NodeData node in _nodes)
        {
            if (node.ParentId < 0 || !_byId.TryGetValue(node.ParentId, out NodeData? parent))
            {
                continue;
            }
            bool active = _progression.IsPassiveAllocated(node.Id) && _progression.IsPassiveAllocated(parent.Id);
            DrawLine(parent.Center, node.Center, active ? new Color(0.3f, 0.9f, 1f) : new Color(0.2f, 0.25f, 0.34f), active ? 4f : 2f, true);
        }
    }

    public void RefreshNodes()
    {
        foreach (NodeData node in _nodes)
        {
            if (node.Button == null)
            {
                continue;
            }
            bool allocated = _progression.IsPassiveAllocated(node.Id);
            bool available = node.ParentId >= 0 && _progression.CanAllocatePassive(node.Id, node.ParentId);
            node.Button.Disabled = !allocated && !available;
            node.Button.Modulate = allocated
                ? StatColors[(int)node.Stat]
                : (available ? new Color(0.9f, 0.9f, 1f) : new Color(0.36f, 0.39f, 0.48f));
        }
        QueueRedraw();
    }

    private void BuildTree()
    {
        var root = new NodeData
        {
            Id = 0,
            ParentId = -1,
            Stat = PassiveStat.Power,
            Ranks = 0,
            Center = new Vector2(1000f, 1000f),
        };
        AddNode(root, "ORIGIN");

        const int branches = 18;
        const int nodesPerBranch = 20;
        for (int branch = 0; branch < branches; branch++)
        {
            float angle = Mathf.Tau * branch / branches;
            for (int tier = 0; tier < nodesPerBranch; tier++)
            {
                int id = 1 + branch * nodesPerBranch + tier;
                int parentId = tier == 0 ? 0 : id - 1;
                float radius = 78f + tier * 42f;
                float curve = Mathf.Sin(tier * 0.72f + branch) * 0.045f;
                Vector2 center = new Vector2(1000f, 1000f) + new Vector2(Mathf.Cos(angle + curve), Mathf.Sin(angle + curve)) * radius;
                PassiveStat stat = (PassiveStat)((branch + tier / 5) % 4);
                int ranks = (tier + 1) % 5 == 0 ? 3 : 1;
                var node = new NodeData { Id = id, ParentId = parentId, Stat = stat, Ranks = ranks, Center = center };
                AddNode(node, BuildTooltip(node));
            }
        }
    }

    private void AddNode(NodeData node, string tooltip)
    {
        _nodes.Add(node);
        _byId[node.Id] = node;
        float size = node.Ranks >= 3 ? 38f : 30f;
        var button = new Button
        {
            Text = node.Id == 0 ? "✦" : (node.Ranks >= 3 ? "◆" : "•"),
            Position = node.Center - Vector2.One * size * 0.5f,
            Size = Vector2.One * size,
            TooltipText = tooltip,
            FocusMode = FocusModeEnum.None,
        };
        int capturedId = node.Id;
        button.Pressed += () => Allocate(capturedId);
        node.Button = button;
        AddChild(button);
    }

    private void Allocate(int nodeId)
    {
        if (!_byId.TryGetValue(nodeId, out NodeData? node) || node.ParentId < 0)
        {
            return;
        }
        _progression.AllocatePassive(node.Id, node.ParentId, node.Stat, node.Ranks);
        RefreshNodes();
    }

    private static string BuildTooltip(NodeData node)
    {
        string effect = node.Stat switch
        {
            PassiveStat.Power => $"+{node.Ranks * 0.5f:0.0}% damage",
            PassiveStat.Vitality => $"+{node.Ranks * 3} max health",
            PassiveStat.Haste => $"-{node.Ranks * 0.2f:0.0}% cooldown",
            _ => $"+{node.Ranks * 2} max mana",
        };
        return $"Node {node.Id}  •  {node.Stat}\n{effect}\nCost: 1 passive point";
    }
}
