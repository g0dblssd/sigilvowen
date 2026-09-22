using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

public partial class LinkMenuUI : CanvasLayer
{
    private SkillCaster? _caster;
    private Control? _panel;
    private OptionButton? _skillOption;
    private OptionButton? _slotOption;
    private List<CheckBox> _linkChecks = new();
    private Label? _status;
    private bool _open;

    public void Setup(SkillCaster caster)
    {
        _caster = caster;
    }

    public bool IsOpen()
    {
        return _open;
    }

    public override void _Ready()
    {
        Layer = 10;

        _panel = new PanelContainer();
        _panel.CustomMinimumSize = new Vector2(680, 600);
        _panel.Position = new Vector2(48, 35);
        _panel.Visible = false;
        AddChild(_panel);

        var vbox = new VBoxContainer();
        _panel.AddChild(vbox);

        var title = new Label { Text = "LINK FORGE (L) — select a slot, skill, then its modifiers" };
        vbox.AddChild(title);

        var skillLabel = new Label { Text = "Skill:" };
        vbox.AddChild(skillLabel);

        _skillOption = new OptionButton();
        foreach (var s in SkillCatalog.All)
        {
            _skillOption.AddItem($"{s.DisplayName} [{s.Type}/{s.DamageElement}]", SkillCatalog.All.IndexOf(s));
        }
        _skillOption.Selected = 1;
        _skillOption.ItemSelected += OnSkillSelected;
        vbox.AddChild(_skillOption);

        var slotLabel = new Label { Text = "Apply to skill slot:" };
        vbox.AddChild(slotLabel);
        _slotOption = new OptionButton();
        for (int i = 0; i < 10; i++)
        {
            _slotOption.AddItem($"Slot {i + 1}", i);
        }
        _slotOption.ItemSelected += OnSlotSelected;
        vbox.AddChild(_slotOption);

        var linkLabel = new Label { Text = "Links (check):" };
        vbox.AddChild(linkLabel);

        foreach (var l in LinkCatalog.All)
        {
            var cb = new CheckBox { Text = $"{l.DisplayName} — {l.Description}" };
            vbox.AddChild(cb);
            _linkChecks.Add(cb);
        }

        var apply = new Button { Text = "Bind selected links to slot" };
        apply.Pressed += OnApply;
        vbox.AddChild(apply);

        _status = new Label { Text = "Pick skill + links, press Apply." };
        vbox.AddChild(_status);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.L)
            {
                Toggle();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.Escape && _open)
            {
                SetOpen(false);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void Toggle()
    {
        SetOpen(!_open);
    }

    private void SetOpen(bool open)
    {
        _open = open;
        if (_panel != null)
        {
            _panel.Visible = open;
        }
        try
        {
            // Click-to-move needs a visible cursor both in and out of the menu.
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        catch
        {
        }
    }

    private void OnApply()
    {
        if (_caster == null || _skillOption == null || _status == null)
        {
            return;
        }
        int idx = _skillOption.Selected;
        if (idx < 0 || idx >= SkillCatalog.All.Count)
        {
            return;
        }
        var skill = SkillCatalog.All[idx];
        var links = new List<SkillLinkData>();
        for (int i = 0; i < _linkChecks.Count && i < LinkCatalog.All.Count; i++)
        {
            if (_linkChecks[i].ButtonPressed)
            {
                links.Add(LinkCatalog.All[i]);
            }
        }
        int slotIndex = _slotOption?.Selected ?? 0;
        int requiredCharges = links.Count;
        if (slotIndex < _caster.Slots.Count && _caster.Slots[slotIndex].Skill == skill)
        {
            requiredCharges = 0;
            foreach (var link in links)
            {
                if (!_caster.Slots[slotIndex].Links.Contains(link))
                {
                    requiredCharges++;
                }
            }
        }
        if (!_caster.TrySpendLinkCharges(skill, requiredCharges))
        {
            _status.Text = $"NO MIX CHARGES: {skill.DisplayName} has {_caster.GetLinkCharges(skill)}/{SkillData.MaxLinkCharges}; needs {requiredCharges}.";
            return;
        }
        while (_caster.Slots.Count <= slotIndex)
        {
            _caster.Slots.Add(new SkillCaster.Slot(skill, links));
        }
        _caster.Slots[slotIndex] = new SkillCaster.Slot(skill, links);
        _caster.SelectedSlot = slotIndex;
        int chargesLeft = _caster.GetLinkCharges(skill);
        _status.Text = $"BOUND: Slot {slotIndex + 1} = {skill.DisplayName} + {links.Count} links | charges {chargesLeft}/{SkillData.MaxLinkCharges}";
        GD.Print($"[Links] Slot {slotIndex + 1} set to {skill.Id} + {links.Count} links, charges={chargesLeft}");
    }

    private void OnSlotSelected(long selected)
    {
        if (_caster == null || selected < 0 || selected >= _caster.Slots.Count || _skillOption == null)
        {
            return;
        }
        var slot = _caster.Slots[(int)selected];
        int skillIndex = SkillCatalog.All.IndexOf(slot.Skill);
        if (skillIndex >= 0)
        {
            _skillOption.Select(skillIndex);
        }
        for (int i = 0; i < _linkChecks.Count; i++)
        {
            _linkChecks[i].ButtonPressed = slot.Links.Contains(LinkCatalog.All[i]);
        }
    }

    private void OnSkillSelected(long selected)
    {
        if (_status != null && selected >= 0 && selected < SkillCatalog.All.Count)
        {
            var skill = SkillCatalog.All[(int)selected];
            int chargesLeft = _caster?.GetLinkCharges(skill) ?? SkillData.MaxLinkCharges;
            _status.Text = $"Preparing {skill.DisplayName}. Mix charges: {chargesLeft}/{SkillData.MaxLinkCharges}; every new link costs 1.";
        }
    }
}
