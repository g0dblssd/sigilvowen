using Godot;

namespace Sigilwoven;

/// <summary>Repeatable arena dungeon: dormant packs, guardian, then a link unlock.</summary>
public partial class DungeonController : Node
{
    private PlayerController? _player;
    private Node3D? _world;
    private Button? _startButton;
    private Label? _statusLabel;
    private bool _unlocked;
    private bool _running;
    private int _packsRemaining;
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
        var title = new Label { Text = "ECHOING VAULT  //  DUNGEON" };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.55f, 0.85f, 1f);
        content.AddChild(title);

        var description = new Label
        {
            Text = "Hunt 3 dormant packs, defeat the guardian, unlock a new Link.",
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
        _packsRemaining = 3;
        _player.GlobalPosition = new Vector3(0f, 0.2f, 0f);
        _player.SetObjectiveStatus("ECHOING VAULT  •  FIND 3 PACKS");
        _player.ShowCombatMessage($"DUNGEON RUN {_runNumber} STARTED");

        SpawnPack("ASHEN CLAW", new Vector3(-11f, 0f, -9f), 86f,
            new[] { EnemyArchetype.Raider, EnemyArchetype.Raider, EnemyArchetype.Brute });
        SpawnPack("VEIL CHOIR", new Vector3(11f, 0f, -8f), 82f,
            new[] { EnemyArchetype.Hexer, EnemyArchetype.Hexer, EnemyArchetype.Raider });
        SpawnPack("IRON VOW", new Vector3(0f, 0f, 13f), 94f,
            new[] { EnemyArchetype.Brute, EnemyArchetype.Raider, EnemyArchetype.Hexer, EnemyArchetype.Raider });
        RefreshPanel();
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
            _player?.SetObjectiveStatus($"ECHOING VAULT  •  {_packsRemaining} PACKS REMAIN");
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Dungeon active: {_packsRemaining} packs remain.";
            }
            return;
        }
        SpawnGuardian();
    }

    private void SpawnGuardian()
    {
        if (_player == null || _world == null)
        {
            return;
        }
        var guardian = new Enemy();
        guardian.Configure(620f + (_runNumber - 1) * 85f, false, EnemyArchetype.Guardian);
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
        SkillLinkData? reward = _player.Progression.UnlockNextLink();
        _player.RestoreMana(PlayerController.MaxMana);
        _player.SetObjectiveStatus(reward == null ? "VAULT CLEARED  •  ALL LINKS OWNED" : $"VAULT CLEARED  •  {reward.DisplayName} UNLOCKED");
        _player.ShowCombatMessage(reward == null ? "GUARDIAN DEFEATED — PARAGON XP" : $"NEW LINK: {reward.DisplayName.ToUpperInvariant()}");
        _running = false;
        _runNumber++;
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        if (_startButton != null)
        {
            _startButton.Disabled = !_unlocked || _running;
            _startButton.Text = _running ? "DUNGEON IN PROGRESS" : $"ENTER ECHOING VAULT  //  RUN {_runNumber}";
        }
        if (_statusLabel != null && !_running)
        {
            _statusLabel.Text = _unlocked
                ? "Ready. Every clear unlocks the next sealed Link."
                : "Locked: clear the three surface packs first.";
        }
    }
}
