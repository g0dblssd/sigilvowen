using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

public partial class LinkMenuUI : CanvasLayer
{
    private SkillCaster? _caster;
    private PlayerProgression? _progression;
    private Control? _panel;
    private OptionButton? _skillOption;
    private OptionButton? _slotOption;
    private List<CheckBox> _linkChecks = new();
    private Label? _status;
    private TextureRect? _skillPreview;
    private Label? _skillDescription;
    private Label? _linkHeader;
    private bool _open;

    public void Setup(SkillCaster caster, PlayerProgression progression)
    {
        _caster = caster;
        _progression = progression;
        _progression.LinkUnlocked += OnLinkUnlocked;
        _progression.Changed += OnProgressionChanged;
        RefreshLinkAvailability();
        RefreshSkillAvailability();
    }

    public bool IsOpen()
    {
        return _open;
    }

    public override void _Ready()
    {
        Layer = 10;
        _panel = new PanelContainer
        {
            AnchorLeft = 0.12f,
            AnchorTop = 0.07f,
            AnchorRight = 0.88f,
            AnchorBottom = 0.93f,
            Visible = false,
        };
        _panel.AddThemeStyleboxOverride("panel", MakePanel(new Color(0.012f, 0.018f, 0.04f, 0.98f), new Color(0.22f, 0.65f, 0.92f), 14, 2));
        AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(vbox);
        var title = new Label { Text = "◆  SIGIL LINK FORGE  ◆", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.48f, 0.88f, 1f) };
        title.AddThemeFontSizeOverride("font_size", 25);
        vbox.AddChild(title);
        var subtitle = new Label { Text = "WEAVE A SKILL, SUPPORT LINKS AND HOTBAR SLOT INTO ONE CAST", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.5f, 0.58f, 0.72f) };
        subtitle.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(subtitle);

        var selectors = new HBoxContainer();
        selectors.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(selectors);
        var skillColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        skillColumn.AddChild(new Label { Text = "ACTIVE SKILL", Modulate = new Color(0.75f, 0.84f, 1f) });
        _skillOption = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var s in SkillCatalog.All)
        {
            _skillOption.AddItem($"{s.DisplayName} [{s.Type}/{s.DamageElement}]", SkillCatalog.All.IndexOf(s));
        }
        _skillOption.Selected = 0;
        _skillOption.ItemSelected += OnSkillSelected;
        skillColumn.AddChild(_skillOption);
        selectors.AddChild(skillColumn);
        RefreshSkillAvailability();

        var slotColumn = new VBoxContainer { CustomMinimumSize = new Vector2(190f, 0f) };
        slotColumn.AddChild(new Label { Text = "HOTBAR DESTINATION", Modulate = new Color(1f, 0.76f, 0.32f) });
        _slotOption = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        for (int i = 0; i < 10; i++)
        {
            string[] keys = { "1", "2", "3", "4", "5", "6", "Z", "X", "C", "V" };
            _slotOption.AddItem($"SLOT {i + 1}  [{keys[i]}]", i);
        }
        _slotOption.ItemSelected += OnSlotSelected;
        slotColumn.AddChild(_slotOption);
        selectors.AddChild(slotColumn);

        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(body);
        var previewPanel = new PanelContainer { CustomMinimumSize = new Vector2(330f, 0f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        previewPanel.AddThemeStyleboxOverride("panel", MakePanel(new Color(0.025f, 0.04f, 0.075f, 0.94f), new Color(0.18f, 0.36f, 0.58f), 9, 1));
        body.AddChild(previewPanel);
        var previewBox = new VBoxContainer();
        previewPanel.AddChild(previewBox);
        _skillPreview = new TextureRect { CustomMinimumSize = new Vector2(180f, 180f), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        previewBox.AddChild(_skillPreview);
        _skillDescription = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        previewBox.AddChild(_skillDescription);

        var linkPanel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        linkPanel.AddThemeStyleboxOverride("panel", MakePanel(new Color(0.02f, 0.03f, 0.055f, 0.96f), new Color(0.32f, 0.22f, 0.58f), 9, 1));
        body.AddChild(linkPanel);
        var linkBox = new VBoxContainer();
        linkPanel.AddChild(linkBox);
        _linkHeader = new Label { Text = "SUPPORT LINKS  •  SELECT UP TO 2", Modulate = new Color(0.78f, 0.58f, 1f) };
        _linkHeader.AddThemeFontSizeOverride("font_size", 16);
        linkBox.AddChild(_linkHeader);
        var linkScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        linkBox.AddChild(linkScroll);
        var checks = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        linkScroll.AddChild(checks);
        foreach (var l in LinkCatalog.All)
        {
            var cb = new CheckBox { Text = $"◇  {l.DisplayName}", TooltipText = l.Description, CustomMinimumSize = new Vector2(0f, 34f) };
            cb.Toggled += _ => RefreshPreview();
            checks.AddChild(cb);
            _linkChecks.Add(cb);
        }
        RefreshLinkAvailability();

        var apply = new Button { Text = "WEAVE INTO SELECTED SLOT", CustomMinimumSize = new Vector2(0f, 48f) };
        apply.AddThemeFontSizeOverride("font_size", 16);
        apply.Pressed += OnApply;
        vbox.AddChild(apply);
        _status = new Label { Text = "Select a skill and compatible support links.", HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(0f, 46f), Modulate = new Color(0.65f, 0.75f, 0.9f) };
        vbox.AddChild(_status);
        RefreshPreview();
    }

    public override void _ExitTree()
    {
        if (_progression != null)
        {
            _progression.LinkUnlocked -= OnLinkUnlocked;
            _progression.Changed -= OnProgressionChanged;
        }
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
        if (open)
        {
            RefreshSlotLabels();
            RefreshSkillAvailability();
            RefreshLinkAvailability();
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
        if (_progression != null && !skill.IsAllowedFor(_progression.HeroClass))
        {
            _status.Text = $"CLASS LOCKED: {skill.DisplayName} belongs to {skill.RequiredClass}.";
            return;
        }
        if (_progression != null && !_progression.IsSkillUnlocked(skill.Id))
        {
            _status.Text = $"LOCKED: {skill.DisplayName} unlocks at level {_progression.GetSkillRequiredLevel(skill.Id)}.";
            return;
        }
        var links = new List<SkillLinkData>();
        for (int i = 0; i < _linkChecks.Count && i < LinkCatalog.All.Count; i++)
        {
            if (_linkChecks[i].ButtonPressed)
            {
                if (_progression != null && !_progression.IsLinkUnlocked(LinkCatalog.All[i].Id))
                {
                    _status.Text = $"LOCKED: defeat a dungeon guardian to unlock {LinkCatalog.All[i].DisplayName}.";
                    return;
                }
                links.Add(LinkCatalog.All[i]);
            }
        }
        int slotIndex = _slotOption?.Selected ?? 0;
        if (!_caster.TryBindSlot(slotIndex, skill, links, out string error))
        {
            _status.Text = $"BIND FAILED\n{error}";
            _status.Modulate = new Color(1f, 0.38f, 0.3f);
            return;
        }
        int chargesLeft = _caster.GetLinkCharges(skill);
        _status.Text = $"◆ BOUND TO SLOT {slotIndex + 1}\n{skill.DisplayName} + {links.Count} links  •  mix charges {chargesLeft}/{SkillData.MaxLinkCharges}";
        _status.Modulate = new Color(0.42f, 1f, 0.68f);
        RefreshSlotLabels();
        GD.Print($"[Links] Slot {slotIndex + 1} set to {skill.Id} + {links.Count} links, charges={chargesLeft}");
    }

    private void OnLinkUnlocked(string linkId)
    {
        RefreshLinkAvailability();
        if (_status != null && LinkCatalog.ById(linkId) is SkillLinkData link)
        {
            _status.Text = $"UNLOCKED: {link.DisplayName} is now available.";
        }
    }

    private void OnProgressionChanged()
    {
        RefreshSkillAvailability();
    }

    private void RefreshSkillAvailability()
    {
        if (_skillOption == null)
        {
            return;
        }
        for (int i = 0; i < SkillCatalog.All.Count; i++)
        {
            SkillData skill = SkillCatalog.All[i];
            int requiredLevel = _progression?.GetSkillRequiredLevel(skill.Id) ?? 1;
            bool unlocked = _progression == null || _progression.IsSkillUnlocked(skill.Id);
            bool classAllowed = _progression == null || skill.IsAllowedFor(_progression.HeroClass);
            _skillOption.SetItemDisabled(i, !unlocked || !classAllowed);
            _skillOption.SetItemText(i, !classAllowed
                ? $"⛔ {skill.DisplayName} [{skill.RequiredClass}]"
                : unlocked
                ? $"{skill.DisplayName} [{skill.Type}/{skill.DamageElement}]"
                : $"🔒 {skill.DisplayName} [LEVEL {requiredLevel}]");
        }
    }

    private void RefreshLinkAvailability()
    {
        SkillData? selectedSkill = _skillOption != null && _skillOption.Selected >= 0 && _skillOption.Selected < SkillCatalog.All.Count
            ? SkillCatalog.All[_skillOption.Selected]
            : null;
        for (int i = 0; i < _linkChecks.Count && i < LinkCatalog.All.Count; i++)
        {
            SkillLinkData link = LinkCatalog.All[i];
            bool unlocked = _progression == null || _progression.IsLinkUnlocked(link.Id);
            string? incompatibility = selectedSkill == null ? null : LinkSystem.GetIncompatibilityReason(selectedSkill, link);
            bool compatible = incompatibility == null;
            _linkChecks[i].Disabled = !unlocked || !compatible;
            _linkChecks[i].Text = !unlocked
                ? $"🔒  {link.DisplayName}  •  Guardian reward"
                : !compatible
                    ? $"×  {link.DisplayName}  •  incompatible"
                    : $"◇  {link.DisplayName}";
            _linkChecks[i].TooltipText = incompatibility ?? link.Description;
            if (!unlocked || !compatible)
            {
                _linkChecks[i].ButtonPressed = false;
            }
        }
        RefreshPreview();
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
        RefreshLinkAvailability();
        RefreshPreview();
    }

    private void OnSkillSelected(long selected)
    {
        if (_status != null && selected >= 0 && selected < SkillCatalog.All.Count)
        {
            var skill = SkillCatalog.All[(int)selected];
            int chargesLeft = _caster?.GetLinkCharges(skill) ?? SkillData.MaxLinkCharges;
            _status.Text = $"{skill.DisplayName.ToUpperInvariant()}  •  {chargesLeft}/{SkillData.MaxLinkCharges} MIX CHARGES AVAILABLE";
            _status.Modulate = new Color(0.65f, 0.75f, 0.9f);
            RefreshLinkAvailability();
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        if (_skillOption == null || _skillDescription == null || _skillPreview == null || _skillOption.Selected < 0 || _skillOption.Selected >= SkillCatalog.All.Count) return;
        SkillData skill = SkillCatalog.All[_skillOption.Selected];
        var selectedLinks = new List<SkillLinkData>();
        for (int i = 0; i < _linkChecks.Count && i < LinkCatalog.All.Count; i++)
        {
            if (_linkChecks[i].ButtonPressed) selectedLinks.Add(LinkCatalog.All[i]);
        }
        ResolvedCast resolved = LinkSystem.ResolveCast(skill, selectedLinks);
        string effects = selectedLinks.Count == 0 ? "No support effects socketed." : string.Join("\n", selectedLinks.ConvertAll(link => $"◆ {link.DisplayName}: {link.Description}"));
        _skillPreview.Texture = SkillIconCatalog.Get(skill.Id);
        _skillDescription.Text = $"{skill.BuildDescription()}\n\nFINAL LINKED CAST\nDamage: {resolved.Damage:0.#} {resolved.Element}\nCooldown multiplier: ×{resolved.CooldownMultiplier:0.00}\n\n{effects}";
        if (_linkHeader != null)
        {
            _linkHeader.Text = $"SUPPORT LINKS  •  {selectedLinks.Count}/{SkillData.MaxLinkCharges} SOCKETED";
            _linkHeader.Modulate = selectedLinks.Count > SkillData.MaxLinkCharges ? new Color(1f, 0.3f, 0.25f) : new Color(0.78f, 0.58f, 1f);
        }
    }

    private void RefreshSlotLabels()
    {
        if (_slotOption == null || _caster == null) return;
        string[] keys = { "1", "2", "3", "4", "5", "6", "Z", "X", "C", "V" };
        for (int i = 0; i < 10; i++)
        {
            string skillName = i < _caster.Slots.Count ? _caster.Slots[i].Skill.DisplayName : "Empty";
            int linkCount = i < _caster.Slots.Count ? _caster.Slots[i].Links.Count : 0;
            _slotOption.SetItemText(i, $"[{keys[i]}]  {skillName}  {(linkCount > 0 ? $"◆{linkCount}" : "")}");
        }
    }

    private static StyleBoxFlat MakePanel(Color background, Color border, int radius, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = width,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = 14f,
            ContentMarginTop = 12f,
            ContentMarginRight = 14f,
            ContentMarginBottom = 12f,
        };
    }
}
