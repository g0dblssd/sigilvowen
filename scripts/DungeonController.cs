using Godot;

namespace Sigilwoven;

/// <summary>Repeatable multi-floor raid: clear escalating packs, then defeat the guardian.</summary>
public partial class DungeonController : Node
{
    private const int MaxFloors = 3;

    public event System.Action<bool>? RaidStateChanged;

    private PlayerController? _player;
    private Node3D? _world;
    private Button? _startButton;
    private Label? _statusLabel;
    private bool _unlocked;
    private bool _running;
    private int _packsRemaining;
    private int _currentFloor;
    private int _runNumber = 1;

    public void Setup(PlayerController player, Node3D world)
    {
        _player = player;
        _world = world;
        RefreshPanel();
    }

    public override void _Ready()
    {
        var layer = new CanvasLayer { Layer = 7 };
        AddChild(layer);

        var panel = new PanelContainer
        {
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -330f,
            OffsetRight = -18f,
            OffsetTop = 20f,
            OffsetBottom = 230f,
        };
        layer.AddChild(panel);

        var content = new VBoxContainer();
        panel.AddChild(content);
        var title = new Label { Text = "ECHOING VAULT  //  DESCENDING RAID" };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.55f, 0.85f, 1f);
        content.AddChild(title);

        var description = new Label
        {
            Text = "Descend through 3 floors of enemy packs, defeat the guardian, earn a Link and Skill.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        description.CustomMinimumSize = new Vector2(285f, 54f);
        content.AddChild(description);

        _statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _statusLabel.CustomMinimumSize = new Vector2(285f, 48f);
        content.AddChild(_statusLabel);

        _startButton = new Button { Text = "ENTER ECHOING VAULT" };
        _startButton.Pressed += StartDungeon;
        content.AddChild(_startButton);
        RefreshPanel();
    }

    public void UnlockDungeon()
    {
        _unlocked = true;
        RefreshPanel();
    }

    private void StartDungeon()
    {
        if (!_unlocked || _running || _player == null || _world == null)
        {
            return;
        }
        _running = true;
        _currentFloor = 1;
        _player.GlobalPosition = new Vector3(0f, 0.2f, 0f);
        RaidStateChanged?.Invoke(true);
        _player.ShowCombatMessage($"RAID RUN {_runNumber} STARTED");
        SpawnRaidFloor();
        RefreshPanel();
    }

    private void SpawnRaidFloor()
    {
        if (_player == null)
        {
            return;
        }

        int packCount = 2 + _currentFloor;
        int membersPerPack = 3 + _currentFloor;
        float baseHp = 70f + _currentFloor * 24f + (_runNumber - 1) * 18f;
        float radius = 15f + _currentFloor * 3f;
        _packsRemaining = packCount;

        for (int packIndex = 0; packIndex < packCount; packIndex++)
        {
            float angle = Mathf.Tau * packIndex / packCount + _currentFloor * 0.31f;
            var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            var archetypes = new EnemyArchetype[membersPerPack];
            for (int memberIndex = 0; memberIndex < membersPerPack; memberIndex++)
            {
                archetypes[memberIndex] = ((packIndex + memberIndex + _currentFloor) % 4) switch
                {
                    0 => EnemyArchetype.Brute,
                    1 => EnemyArchetype.Hexer,
                    _ => EnemyArchetype.Raider,
                };
            }
            SpawnPack($"FLOOR {_currentFloor} // PACK {packIndex + 1}", position, baseHp, archetypes);
        }

        _player.SetObjectiveStatus($"RAID FLOOR {_currentFloor}/{MaxFloors}  •  {_packsRemaining} PACKS");
        _player.ShowCombatMessage($"FLOOR {_currentFloor} — THE HORDE STIRS");
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Floor {_currentFloor}/{MaxFloors}: {_packsRemaining} packs remain.";
        }
        GD.Print($"[Raid] Run {_runNumber}, floor {_currentFloor}: {packCount} packs, {membersPerPack} enemies each.");
    }

    private void SpawnPack(string name, Vector3 position, float baseHp, EnemyArchetype[] archetypes)
    {
        if (_player == null || _world == null)
        {
            return;
        }
        var pack = new MobPack();
        pack.Setup(_player, name, 5.2f);
        for (int i = 0; i < archetypes.Length; i++)
        {
            float angle = Mathf.Tau * i / archetypes.Length;
            pack.AddMember(baseHp, archetypes[i], new Vector3(Mathf.Cos(angle) * 1.7f, 0f, Mathf.Sin(angle) * 1.7f));
        }
        pack.Cleared += OnDungeonPackCleared;
        _world.AddChild(pack);
        pack.GlobalPosition = position;
    }

    private void OnDungeonPackCleared(MobPack pack)
    {
        pack.Cleared -= OnDungeonPackCleared;
        _packsRemaining = Mathf.Max(0, _packsRemaining - 1);
        if (_packsRemaining > 0)
        {
            _player?.SetObjectiveStatus($"RAID FLOOR {_currentFloor}/{MaxFloors}  •  {_packsRemaining} PACKS REMAIN");
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Floor {_currentFloor}/{MaxFloors}: {_packsRemaining} packs remain.";
            }
            return;
        }

        if (_currentFloor < MaxFloors)
        {
            _currentFloor++;
            if (_player != null)
            {
                _player.GlobalPosition = new Vector3(0f, 0.2f, 0f);
                _player.ShowCombatMessage($"DESCENDING TO FLOOR {_currentFloor}");
            }
            SpawnRaidFloor();
        }
        else
        {
            SpawnGuardian();
        }
    }

    private void SpawnGuardian()
    {
        if (_player == null || _world == null)
        {
            return;
        }
        var guardian = new Enemy();
        guardian.Configure(850f + (_runNumber - 1) * 120f, false, EnemyArchetype.Guardian);
        guardian.Died += OnGuardianDied;
        _world.AddChild(guardian);
        guardian.GlobalPosition = new Vector3(0f, 0f, -13f);
        guardian.JoinAmbush(_player);
        _player.SetObjectiveStatus("BOSS  •  SIGIL GUARDIAN");
        _player.ShowCombatMessage("THE SIGIL GUARDIAN AWAKENS");
        if (_statusLabel != null)
        {
            _statusLabel.Text = "Boss active: defeat the Sigil Guardian.";
        }
    }

    private void OnGuardianDied(Enemy guardian)
    {
        guardian.Died -= OnGuardianDied;
        if (_player == null)
        {
            return;
        }
        SkillLinkData? linkReward = _player.Progression.UnlockNextLink();
        SkillData? skillReward = _player.Progression.UnlockNextSkill();
        _player.RestoreMana(_player.MaxMana);
        string rewardSummary = BuildRewardSummary(linkReward, skillReward);
        _player.SetObjectiveStatus($"RAID CLEARED  •  {rewardSummary}");
        _player.ShowCombatMessage(rewardSummary);
        _running = false;
        _runNumber++;
        RaidStateChanged?.Invoke(false);
        RefreshPanel();
    }

    private static string BuildRewardSummary(SkillLinkData? linkReward, SkillData? skillReward)
    {
        if (linkReward != null && skillReward != null)
        {
            return $"LINK: {linkReward.DisplayName.ToUpperInvariant()}  •  SKILL: {skillReward.DisplayName.ToUpperInvariant()}";
        }
        if (linkReward != null)
        {
            return $"NEW LINK: {linkReward.DisplayName.ToUpperInvariant()}";
        }
        if (skillReward != null)
        {
            return $"NEW SKILL: {skillReward.DisplayName.ToUpperInvariant()}";
        }
        return "GUARDIAN DEFEATED — PARAGON XP";
    }

    private void RefreshPanel()
    {
        if (_startButton != null)
        {
            _startButton.Disabled = !_unlocked || _running;
            _startButton.Text = _running ? $"RAID FLOOR {_currentFloor}/{MaxFloors}" : $"START DESCENDING RAID  //  RUN {_runNumber}";
        }
        if (_statusLabel != null && !_running)
        {
            _statusLabel.Text = _unlocked
                ? "Ready. Every clear grants the next sealed Link and Skill."
                : "Locked: purge every surface camp once.";
        }
    }
}
