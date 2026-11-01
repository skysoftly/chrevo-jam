
using Godot;
using game.scenes.world;

namespace game.scenes.bosses;

public partial class PumpkinKing : Boss
{
    private enum BossState
    {
        Intro,
        IntroWalk,
        Moving,
        CircleAttack,
        Summoning
    }


    [ExportGroup("Intro")]
    [Export] private float _introWalkDuration = 3f;

    private float _introWalkTimer;

    [ExportGroup("Projectile")]
    [Export] private PackedScene _projectileScene;
    [Export] private float _projectileInterval = 1.5f;

    [ExportGroup("Minions")]
    [Export] private PackedScene _skeletonScene;
    [Export] private int _skeletonCount = 3;
    [Export] private float _summonInterval = 0.5f;
    [Export] private float _summonRadius = 100f;

    [ExportGroup("Animations")]
    [Export] private string _idleAnimation = "Idle";
    [Export] private string _walkAnimation = "Walk";
    [Export] private string _attackAnimation = "Attack";

    [ExportGroup("Movement")]
    [Export] private float _moveSpeed = 55f;
    [Export] private float _minMoveDistance = 30f;

    [ExportGroup("Arena Movement")]
    [Export] private Vector2 _arenaMin = new(-450, -300);
    [Export] private Vector2 _arenaMax = new(450, 300);
    [Export] private float _centerAreaX = 180f;
    [Export] private float _centerAreaY = 120f;
    [Export] private float _centerPreference = 0.8f;

    [ExportGroup("Circle Attack")]
    [Export] private string _circleAttackAnimation = "Get ready to attack";
    [Export] private float _circlePreparationDuration = 1f;
    [Export] private float _circleAttackInterval = 9f;
    [Export] private int _circleShotCount = 12;
    [Export] private float _circleShotInterval = 0.2f;

    [ExportGroup("Summoning")]
    [Export] private float _summonChance = 0.15f;
    [Export] private float _summonDuration = 3f;

    [ExportGroup("Combat")]
    [Export] public float AttackRange { get; set; } = 50f;

    private BossState _state = BossState.Intro;

    private Vector2 _targetPosition;

    private float _projectileTimer;
    private float _circleTimer;
    private float _actionTimer;
    private float _summonTimer;

    private int _circleShotsSpawned;
    private int _skeletonsSpawned;
    private int _circlePattern;

    // --------------------------------------------------
    // INTRO
    // --------------------------------------------------

    protected override void OnIntroFinished()
    {
        _state = BossState.IntroWalk;
        _introWalkTimer = _introWalkDuration;

        ChooseNewTarget();
        PlayAnimation(_walkAnimation);
    }

    // --------------------------------------------------
    // PROCESS
    // --------------------------------------------------

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (IsDead || _state == BossState.Intro)
            return;

        float dt = (float)delta;

        _projectileTimer -= dt;
        _circleTimer -= dt;
        _actionTimer -= dt;
        _summonTimer -= dt;

        switch (_state)
        {
            case BossState.IntroWalk:
                _introWalkTimer -= dt;

                if (_introWalkTimer <= 0f)
                    StartMoving();

                break;

            case BossState.Moving:
                UpdateMoving();
                break;

            case BossState.CircleAttack:
                UpdateCircleAttack();
                break;

            case BossState.Summoning:
                UpdateSummoning();
                break;
        }
    }

    // --------------------------------------------------
    // PHYSICS
    // --------------------------------------------------

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
            return;

        if (_state != BossState.Moving &&
            _state != BossState.IntroWalk)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 direction = GlobalPosition.DirectionTo(_targetPosition);

        Velocity = direction * _moveSpeed;

        if (GodotObject.IsInstanceValid(Sprite))
        {
            if (direction.X > 0f)
                Sprite.FlipH = false;
            else if (direction.X < 0f)
                Sprite.FlipH = true;
        }

        MoveAndSlide();

        // Если цель достигнута — выбираем следующую.
        if (GlobalPosition.DistanceSquaredTo(_targetPosition) <=
            _minMoveDistance * _minMoveDistance)
        {
            ChooseNewTarget();
        }
    }

    // --------------------------------------------------
    // MOVEMENT
    // --------------------------------------------------

    private void StartMoving()
    {
        _state = BossState.Moving;

        _projectileTimer = _projectileInterval;
        _circleTimer = _circleAttackInterval;

        ChooseNewTarget();

        PlayAnimation(_walkAnimation);
    }

    private void UpdateMoving()
    {
        if (_projectileTimer <= 0f)
        {
            ShootAtPlayer();
            _projectileTimer = _projectileInterval;
        }

        if (_circleTimer <= 0f)
        {
            if (GD.Randf() < _summonChance)
                StartSummoning();
            else
                StartCircleAttack();
        }
    }

    private void ChooseNewTarget()
    {
        Vector4 bounds = Spawner.Arena;

        bool stayNearCenter = GD.Randf() < _centerPreference;

        Vector2 min = stayNearCenter
            ? new Vector2(-_centerAreaX, -_centerAreaY)
            : new Vector2(bounds.X, bounds.Y);

        Vector2 max = stayNearCenter
            ? new Vector2(_centerAreaX, _centerAreaY)
            : new Vector2(bounds.Z, bounds.W);

        Vector2 randomPoint = new(
            (float)GD.RandRange(min.X, max.X),
            (float)GD.RandRange(min.Y, max.Y)
        );

        _targetPosition = Spawner.ToGlobal(randomPoint);
    }

    // --------------------------------------------------
    // PROJECTILES
    // --------------------------------------------------

    private void ShootAtPlayer()
    {
        if (!GodotObject.IsInstanceValid(Player))
            return;

        Vector2 direction =
            GlobalPosition.DirectionTo(Player.GlobalPosition);

        SpawnProjectile(direction);
    }

    private void SpawnProjectile(Vector2 direction)
    {
        if (_projectileScene == null ||
            !GodotObject.IsInstanceValid(Spawner))
            return;

        if (_projectileScene.Instantiate() is not Projectile projectile)
            return;

        Spawner.AddBossObject(projectile);

        projectile.GlobalPosition = GlobalPosition;
        projectile.SetDirection(direction);
    }

    // --------------------------------------------------
    // CIRCLE ATTACK
    // --------------------------------------------------

    private void StartCircleAttack()
    {
        _state = BossState.CircleAttack;

        Velocity = Vector2.Zero;

        _circleShotsSpawned = 0;
        _actionTimer = _circlePreparationDuration;

        _circlePattern = GD.RandRange(0, 2);

        PlayAnimation(_circleAttackAnimation);
    }

    private void UpdateCircleAttack()
    {
        if (_circleShotsSpawned >= _circleShotCount)
        {
            StartMoving();
            return;
        }

        if (_actionTimer > 0f)
            return;

        SpawnCircleShot();

        _circleShotsSpawned++;
        _actionTimer = _circleShotInterval;
    }

    private void SpawnCircleShot()
    {
        switch (_circlePattern)
        {
            case 0:
            {
                float angle = Mathf.Tau *
                    _circleShotsSpawned / _circleShotCount;

                SpawnProjectile(Vector2.Right.Rotated(angle));
                break;
            }

            case 1:
            {
                if (!GodotObject.IsInstanceValid(Player))
                    return;

                Vector2 baseDirection =
                    GlobalPosition.DirectionTo(Player.GlobalPosition);

                float spread = Mathf.DegToRad(100f);

                float progress = _circleShotsSpawned /
                    (float)Mathf.Max(1, _circleShotCount - 1);

                float angle = Mathf.Lerp(
                    -spread / 2f,
                    spread / 2f,
                    progress
                );

                SpawnProjectile(baseDirection.Rotated(angle));
                break;
            }

            case 2:
            {
                float angle = Mathf.Tau *
                    _circleShotsSpawned / _circleShotCount;

                SpawnProjectile(Vector2.Right.Rotated(angle));

                SpawnProjectile(
                    Vector2.Right.Rotated(angle + Mathf.Pi)
                );

                break;
            }
        }
    }

    // --------------------------------------------------
    // SUMMONING
    // --------------------------------------------------

    private void StartSummoning()
    {
        _state = BossState.Summoning;

        Velocity = Vector2.Zero;

        _summonTimer = _summonDuration;
        _actionTimer = 0f;

        _skeletonsSpawned = 0;

        PlayAnimation(_attackAnimation);
    }

    private void UpdateSummoning()
    {
        if (_summonTimer <= 0f ||
            _skeletonsSpawned >= _skeletonCount)
        {
            StartMoving();
            return;
        }

        if (_actionTimer > 0f)
            return;

        SpawnSkeleton();

        _skeletonsSpawned++;
        _actionTimer = _summonInterval;
    }

    private void SpawnSkeleton()
    {
        if (_skeletonScene == null ||
            !GodotObject.IsInstanceValid(Spawner))
            return;

        Vector2 offset = new(
            (float)GD.RandRange(-_summonRadius, _summonRadius),
            (float)GD.RandRange(-_summonRadius, _summonRadius)
        );

        Spawner.SpawnBossMinion(
            _skeletonScene,
            GlobalPosition + offset
        );
    }

    // --------------------------------------------------
    // ANIMATIONS
    // --------------------------------------------------

    private void PlayAnimation(string animation)
    {
        if (!GodotObject.IsInstanceValid(AnimationPlayer))
            return;

        if (!AnimationPlayer.HasAnimation(animation))
        {
            GD.PushWarning(
                $"{Name}: Animation '{animation}' not found!"
            );
            return;
        }

        AnimationPlayer.Play(animation);
    }

    // --------------------------------------------------
    // DEATH
    // --------------------------------------------------

    protected override void Die()
    {
        if (IsDead)
            return;

        Velocity = Vector2.Zero;

        base.Die();
    }
}
