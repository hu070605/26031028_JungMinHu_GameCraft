// 이미지나 키보드에 의존하지 않는 라운드·생성·전투 규칙입니다.
enum MonsterKind { Thorn, Heart }

sealed class MonsterState
{
	public MonsterKind Kind { get; }
	public int Floor { get; }
	public float Left { get; internal set; }
	public float Right => Left + GameplaySession.MonsterWidth;
	public bool IsDying { get; private set; }
	public double DeathTime { get; internal set; }
	public int DeathFrame => Math.Min((int)(DeathTime * GameplaySession.DeathFramesPerSecond), 7);

	public MonsterState(MonsterKind kind, int floor, float left)
	{
		Kind = kind;
		Floor = floor;
		Left = left;
	}

	internal void BeginDeath()
	{
		IsDying = true;
		DeathTime = 0.0;
	}
}

sealed class GameplaySession
{
	public const int FloorCount = 3;
	public const int MonstersPerRound = 50;
	public const float MonsterWidth = 76.0f;
	public const double AttackFramesPerSecond = 24.0;
	public const double AttackDuration = 8.0 / AttackFramesPerSecond;
	public const double DeathFramesPerSecond = 20.0;
	public const double DeathDuration = 8.0 / DeathFramesPerSecond;
	public const float InitialSpeed = 150.0f;
	public const float SpeedIncreasePerRound = 15.0f;
	private const double HeartProbability = 0.20;
	private const double SimulationStep = 1.0 / 120.0;
	private const double FloorChangeSeconds = 0.18;
	private const double FloorChangeBuffer = 0.12;

	private readonly Random _random;
	private readonly float _spawnLeft;
	private readonly float _blueLeft;
	private readonly float _blueWidth;
	private readonly float _playerBodyLeft;
	private readonly float _playerBodyWidth;
	private readonly List<MonsterState> _monsters = new();
	private int _lastSpawnFloor;
	private int _nextSpawnFloor;
	private int _remainingInLane;
	private double _distanceSinceSpawn;
	private double _attackTime = -1.0;
	private bool _attackConsumed;

	public IReadOnlyList<MonsterState> Monsters => _monsters;
	public int Round { get; private set; }
	public int SpawnedThisRound { get; private set; }
	public int ResolvedThisRound { get; private set; }
	public int Health { get; private set; }
	public int PlayerFloor { get; private set; }
	public float Speed => InitialSpeed + (Round - 1) * SpeedIncreasePerRound;
	public bool IsGameOver => Health <= 0;
	public bool IsAttacking => _attackTime >= 0.0 && _attackTime < AttackDuration;
	public int AttackFrame => IsAttacking ? Math.Min((int)(_attackTime * AttackFramesPerSecond), 7) : 0;
	public double HealthFeedbackTime { get; private set; }
	public int LastHealthChange { get; private set; }

	public GameplaySession(float spawnLeft, float blueLeft, float blueWidth,
		float playerBodyLeft, float playerBodyWidth, int? randomSeed = null)
	{
		_spawnLeft = spawnLeft;
		_blueLeft = blueLeft;
		_blueWidth = blueWidth;
		_playerBodyLeft = playerBodyLeft;
		_playerBodyWidth = playerBodyWidth;
		_random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
		Reset();
	}

	public void Reset()
	{
		Round = 1;
		Health = 3;
		PlayerFloor = 0;
		_attackTime = -1.0;
		_attackConsumed = false;
		HealthFeedbackTime = 0.0;
		LastHealthChange = 0;
		BeginRound();
	}

	public void MovePlayer(int direction)
	{
		if (!IsGameOver)
		{
			PlayerFloor = Math.Clamp(PlayerFloor + direction, 0, FloorCount - 1);
		}
	}

	public void RequestAttack()
	{
		if (!IsGameOver && !IsAttacking)
		{
			_attackTime = 0.0;
			_attackConsumed = false;
		}
	}

	public void Update(double deltaTime)
	{
		if (!double.IsFinite(deltaTime) || deltaTime <= 0.0)
		{
			return;
		}
		// 긴 프레임에도 공격 구간이나 몸 접촉을 건너뛰지 않도록 작은 단위로 계산합니다.
		while (deltaTime > 0.0000001)
		{
			double step = Math.Min(SimulationStep, deltaTime);
			Step(step);
			deltaTime -= step;
		}
	}

	private void Step(double deltaTime)
	{
		HealthFeedbackTime = Math.Max(0.0, HealthFeedbackTime - deltaTime);
		UpdateDeathAnimations(deltaTime);
		if (IsGameOver)
		{
			return;
		}

		if (IsAttacking)
		{
			_attackTime += deltaTime;
		}
		float movement = Speed * (float)deltaTime;
		foreach (MonsterState monster in _monsters)
		{
			if (!monster.IsDying)
			{
				monster.Left -= movement;
			}
		}

		ResolvePunch();
		if (IsGameOver)
		{
			return;
		}
		ResolveContactAndMisses();
		if (IsGameOver)
		{
			return;
		}

		_distanceSinceSpawn += movement;
		// 세 포탈은 이 생성기 하나를 공유합니다. 앞 몬스터와의 거리가 기준입니다.
		while (SpawnedThisRound < MonstersPerRound && _distanceSinceSpawn >= RequiredSpawnDistance())
		{
			double requiredDistance = RequiredSpawnDistance();
			_distanceSinceSpawn -= requiredDistance;
			SpawnMonster(_nextSpawnFloor, _spawnLeft - (float)_distanceSinceSpawn);
		}

		// 마지막 몬스터의 터지는 애니메이션까지 끝나야 다음 라운드를 시작합니다.
		if (SpawnedThisRound == MonstersPerRound && _monsters.Count == 0)
		{
			Round++;
			BeginRound();
		}
	}

	private void BeginRound()
	{
		_monsters.Clear();
		SpawnedThisRound = 0;
		ResolvedThisRound = 0;
		_distanceSinceSpawn = 0.0;
		_remainingInLane = 0;
		SpawnMonster(_random.Next(FloorCount), _spawnLeft);
	}

	private void SpawnMonster(int floor, float left)
	{
		MonsterKind kind = _random.NextDouble() < HeartProbability ? MonsterKind.Heart : MonsterKind.Thorn;
		_monsters.Add(new MonsterState(kind, floor, left));
		SpawnedThisRound++;
		_lastSpawnFloor = floor;
		ChooseNextFloor();
	}

	private void ChooseNextFloor()
	{
		// 같은 층에는 3~5마리씩 한 몸 너비로 이어서 내보내고, 다음 묶음은 다른 층입니다.
		if (_remainingInLane == 0)
		{
			_remainingInLane = _random.Next(3, 6) - 1;
			_nextSpawnFloor = _lastSpawnFloor;
		}
		else
		{
			_remainingInLane--;
			_nextSpawnFloor = _remainingInLane > 0
				? _lastSpawnFloor
				: (_lastSpawnFloor + 1 + _random.Next(FloorCount - 1)) % FloorCount;
		}
	}

	private double RequiredSpawnDistance()
	{
		if (_nextSpawnFloor == _lastSpawnFloor)
		{
			return MonsterWidth;
		}
		int floorSteps = Math.Abs(_nextSpawnFloor - _lastSpawnFloor);
		// 앞 몬스터 전체가 파란 구역을 벗어난 뒤 다음 층의 몬스터가 진입합니다.
		// 속도가 올라가도 최대 두 층을 이동할 시간은 거리로 환산해 확보합니다.
		return _blueWidth + MonsterWidth + Speed * (floorSteps * FloorChangeSeconds + FloorChangeBuffer);
	}

	private void ResolvePunch()
	{
		// 주먹이 뻗는 4~5번째 프레임에만, 한 번의 공격당 가까운 몬스터 한 마리를 판정합니다.
		if (_attackConsumed || !IsAttacking || _attackTime < 3.0 / AttackFramesPerSecond ||
			_attackTime >= 5.0 / AttackFramesPerSecond)
		{
			return;
		}
		MonsterState? target = null;
		foreach (MonsterState monster in _monsters)
		{
			if (monster.IsDying || monster.Floor != PlayerFloor ||
				monster.Left >= _blueLeft + _blueWidth || monster.Right <= _blueLeft)
			{
				continue;
			}
			if (target == null || monster.Left < target.Left)
			{
				target = monster;
			}
		}
		if (target != null)
		{
			_attackConsumed = true;
			target.BeginDeath();
			if (target.Kind == MonsterKind.Heart)
			{
				ChangeHealth(-1);
			}
		}
	}

	private void ResolveContactAndMisses()
	{
		for (int index = _monsters.Count - 1; index >= 0; index--)
		{
			MonsterState monster = _monsters[index];
			if (monster.IsDying)
			{
				continue;
			}
			if (monster.Floor == PlayerFloor && monster.Left < _playerBodyLeft + _playerBodyWidth &&
				monster.Right > _playerBodyLeft)
			{
				monster.BeginDeath();
				ChangeHealth(monster.Kind == MonsterKind.Heart ? 1 : -1);
				if (IsGameOver)
				{
					return;
				}
			}
			else if (monster.Right < 0.0f)
			{
				// 다른 층에서 지나친 몬스터는 피해·회복 없이 화면 밖에서 처리합니다.
				_monsters.RemoveAt(index);
				ResolvedThisRound++;
			}
		}
	}

	private void UpdateDeathAnimations(double deltaTime)
	{
		for (int index = _monsters.Count - 1; index >= 0; index--)
		{
			MonsterState monster = _monsters[index];
			if (!monster.IsDying)
			{
				continue;
			}
			monster.DeathTime += deltaTime;
			if (monster.DeathTime >= DeathDuration)
			{
				_monsters.RemoveAt(index);
				ResolvedThisRound++;
			}
		}
	}

	private void ChangeHealth(int amount)
	{
		Health = Math.Max(0, Health + amount);
		LastHealthChange = amount;
		HealthFeedbackTime = 0.35;
	}
}
