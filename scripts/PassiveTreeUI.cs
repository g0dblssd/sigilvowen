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

        var title = new Label { Text = "SIGIL CONSTELLATION  //  1009 PASSIVE NODES" };
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
            Text = "Open the constellation to weave its 1009 passive nodes.",
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
        else if (@event is InputEventKey escape && escape.Pressed && !escape.Echo && escape.Keycode == Key.Escape && IsOpen())
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    public bool IsOpen()
    {
        return _panel?.Visible == true;
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
        _treeScroll.ScrollHorizontal = 1500;
        _treeScroll.ScrollVertical = 1500;
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
    }

    private PlayerProgression _progression = null!;
    private readonly List<NodeData> _nodes = new();
    private readonly Dictionary<int, NodeData> _byId = new();
    private PanelContainer? _hoverPanel;
    private Label? _hoverLabel;
    private int _hoveredNodeId = -1;
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
        CustomMinimumSize = new Vector2(4000f, 4000f);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        BuildTree();
        BuildHoverPanel();
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

        foreach (NodeData node in _nodes)
        {
            bool allocated = _progression.IsPassiveAllocated(node.Id);
            bool available = node.ParentId >= 0 && _progression.CanAllocatePassive(node.Id, node.ParentId);
            Color color = allocated
                ? StatColors[(int)node.Stat]
                : (available ? new Color(0.88f, 0.9f, 1f) : new Color(0.22f, 0.25f, 0.34f));
            float radius = node.Id == 0 ? 16f : (node.Ranks >= 3 ? 11f : 7f);
            if (node.Id == _hoveredNodeId)
            {
                DrawCircle(node.Center, radius + 5f, new Color(1f, 0.88f, 0.35f, 0.5f));
            }
            DrawCircle(node.Center, radius, color);
            if (node.Ranks >= 3)
            {
                DrawCircle(node.Center, radius * 0.42f, new Color(1f, 0.92f, 0.55f));
            }
        }
    }

    public void RefreshNodes()
    {
        QueueRedraw();
        if (_hoveredNodeId >= 0)
        {
            UpdateHoverText();
        }
    }

    private void BuildTree()
    {
        var root = new NodeData
        {
            Id = 0,
            ParentId = -1,
            Stat = PassiveStat.Power,
            Ranks = 0,
            Center = new Vector2(2000f, 2000f),
        };
        AddNode(root);

        const int branches = 18;
        const int originalNodesPerBranch = 20;
        const int expansionNodesPerBranch = 36;
        for (int branch = 0; branch < branches; branch++)
        {
            float angle = Mathf.Tau * branch / branches;
            for (int tier = 0; tier < originalNodesPerBranch; tier++)
            {
                int id = 1 + branch * originalNodesPerBranch + tier;
                int parentId = tier == 0 ? 0 : id - 1;
                float radius = 95f + tier * 32f;
                float curve = Mathf.Sin(tier * 0.72f + branch) * 0.04f;
                Vector2 center = new Vector2(2000f, 2000f) + new Vector2(Mathf.Cos(angle + curve), Mathf.Sin(angle + curve)) * radius;
                PassiveStat stat = (PassiveStat)((branch + tier / 5) % 4);
                int ranks = (tier + 1) % 5 == 0 ? 3 : 1;
                var node = new NodeData { Id = id, ParentId = parentId, Stat = stat, Ranks = ranks, Center = center };
                AddNode(node);
            }

            for (int extra = 0; extra < expansionNodesPerBranch; extra++)
            {
                int tier = originalNodesPerBranch + extra;
                int id = 361 + branch * expansionNodesPerBranch + extra;
                int parentId = extra == 0
                    ? 1 + branch * originalNodesPerBranch + originalNodesPerBranch - 1
                    : id - 1;
                float radius = 95f + tier * 32f;
                float curve = Mathf.Sin(tier * 0.72f + branch) * 0.04f;
                Vector2 center = new Vector2(2000f, 2000f) + new Vector2(Mathf.Cos(angle + curve), Mathf.Sin(angle + curve)) * radius;
                PassiveStat stat = (PassiveStat)((branch + tier / 5) % 4);
                int ranks = (tier + 1) % 5 == 0 ? 3 : 1;
                AddNode(new NodeData { Id = id, ParentId = parentId, Stat = stat, Ranks = ranks, Center = center });
            }
        }
    }

    private void AddNode(NodeData node)
    {
        _nodes.Add(node);
        _byId[node.Id] = node;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            int found = FindNodeAt(motion.Position);
            if (found != _hoveredNodeId)
            {
                _hoveredNodeId = found;
                UpdateHoverText();
                QueueRedraw();
            }
            if (_hoverPanel != null && _hoverPanel.Visible)
            {
                _hoverPanel.Position = new Vector2(
                    Mathf.Clamp(motion.Position.X + 18f, 0f, Size.X - 330f),
                    Mathf.Clamp(motion.Position.Y + 18f, 0f, Size.Y - 130f));
            }
        }
        else if (@event is InputEventMouseButton click
            && click.ButtonIndex == MouseButton.Left
            && click.Pressed
            && _hoveredNodeId >= 0)
        {
            Allocate(_hoveredNodeId);
            AcceptEvent();
        }
    }

    private void BuildHoverPanel()
    {
        _hoverPanel = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(320f, 112f),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 100,
        };
        _hoverLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _hoverPanel.AddChild(_hoverLabel);
        AddChild(_hoverPanel);
    }

    private int FindNodeAt(Vector2 position)
    {
        int closestId = -1;
        float closestDistance = 18f;
        foreach (NodeData node in _nodes)
        {
            float distance = position.DistanceTo(node.Center);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestId = node.Id;
            }
        }
        return closestId;
    }

    private void UpdateHoverText()
    {
        if (_hoverPanel == null || _hoverLabel == null || !_byId.TryGetValue(_hoveredNodeId, out NodeData? node))
        {
            if (_hoverPanel != null)
            {
                _hoverPanel.Visible = false;
            }
            return;
        }
        _hoverPanel.Visible = true;
        _hoverLabel.Text = BuildTooltip(node);
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

    private string BuildTooltip(NodeData node)
    {
        if (node.Id == 0)
        {
            return "ORIGIN  •  ALLOCATED\nThe center of your Sigil Constellation.\nChoose any connected first node to begin.";
        }
        string effect = node.Stat switch
        {
            PassiveStat.Power => $"+{node.Ranks * 0.5f:0.0}% damage",
            PassiveStat.Vitality => $"+{node.Ranks * 3} max health",
            PassiveStat.Haste => $"-{node.Ranks * 0.2f:0.0}% cooldown",
            _ => $"+{node.Ranks * 2} max mana",
        };
        bool allocated = _progression.IsPassiveAllocated(node.Id);
        bool parentAllocated = _progression.IsPassiveAllocated(node.ParentId);
        string status = allocated
            ? "ALLOCATED"
            : (!parentAllocated ? $"LOCKED — requires connected node #{node.ParentId}" : (_progression.PassivePoints <= 0 ? "READY — requires 1 passive point" : "AVAILABLE — click to allocate"));
        string milestone = node.Ranks >= 3 ? "MILESTONE NODE" : "PASSIVE NODE";
        return $"{milestone} #{node.Id}  •  {node.Stat.ToString().ToUpperInvariant()}\n{effect}\n{status}\nCost: 1 passive point";
    }
}
