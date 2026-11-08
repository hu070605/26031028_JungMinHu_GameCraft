static class Program
{
	private static int _passed;

	static void Main()
	{
		Run("Independent 20% heart probability", TestProbability);
		Run("Body-width distance controls spawning", TestSpawnDistance);
		Run("50 monsters, safe lane changes, endless rounds and increasing speed", TestFullRound);
		Run("Punch only hits current lane inside blue zone", TestAttackRange);
		Run("One hit kills; death animation stays then disappears", TestDeath);
		Run("A swing hits at most one monster", TestSingleHit);
		Run("Heart punch damages exactly once", TestHeartPunch);
		Run("Body contact heals hearts and damages thorns exactly once", TestContact);
		Run("Large frames do not skip contact or attack", TestLargeDelta);
		Run("Game over stops waves; restart resets gameplay", TestGameOver);
		Console.WriteLine($"PASS: {_passed} gameplay rule checks");
	}

	private static void Run(string name, Action test)
	{
		test();
		_passed++;
		Console.WriteLine($"PASS: {name}");
	}

	private static GameplaySession Create(int seed = 5) => new(1171.5f, 33.0f, 160.0f, 89.0f, 48.0f, seed);
	private static void Check(bool condition, string message)
	{
		if (!condition) throw new Exception(message);
	}

	private static GameplaySession WithKind(MonsterKind kind)
	{
		for (int seed = 0; seed < 100; seed++)
		{
			GameplaySession session = Create(seed);
			if (session.Monsters[0].Kind == kind) return session;
		}
		throw new Exception("No suitable seeded fixture");
	}

	private static void TestProbability()
	{
		int hearts = 0;
		for (int seed = 0; seed < 10000; seed++)
		{
			if (Create(seed).Monsters[0].Kind == MonsterKind.Heart) hearts++;
		}
		Check(hearts > 1900 && hearts < 2100, $"Unexpected heart distribution: {hearts}/10000");
		Console.WriteLine($"  Heart sample: {hearts}/10000");
	}

	private static void TestSpawnDistance()
	{
		GameplaySession session = Create();
		session.Update((76.0 - 0.1) / session.Speed);
		Check(session.SpawnedThisRound == 1, "Spawned before 76px clearance");
		session.Update(0.2 / session.Speed);
		Check(session.SpawnedThisRound == 2, "Did not spawn after 76px clearance");
		Check(session.Monsters[0].Floor == session.Monsters[1].Floor, "Initial lane group split unexpectedly");
		Check(Math.Abs(session.Monsters[1].Left - session.Monsters[0].Left - 76.0f) < 0.05f,
			"Monsters were not adjacent at body-width separation");
	}

	private static void AvoidContact(GameplaySession session)
	{
		HashSet<int> danger = session.Monsters.Where(m => !m.IsDying && m.Left < 180.0f && m.Right > 70.0f)
			.Select(m => m.Floor).ToHashSet();
		int safe = Enumerable.Range(0, 3).First(floor => !danger.Contains(floor));
		session.MovePlayer(safe - session.PlayerFloor);
	}

	private static void TestFullRound()
	{
		GameplaySession session = Create(123);
		HashSet<MonsterState> seen = new();
		HashSet<int> lanes = new();
		for (int tick = 0; tick < 18000 && session.Round < 3; tick++)
		{
			if (session.Round == 1)
			{
				foreach (MonsterState monster in session.Monsters) { seen.Add(monster); lanes.Add(monster.Floor); }
			}
			Check(session.SpawnedThisRound <= 50, "More than 50 monsters spawned in a round");
			int threatLanes = session.Monsters.Where(m => !m.IsDying && m.Left < 193.0f && m.Right > 33.0f)
				.Select(m => m.Floor).Distinct().Count();
			Check(threatLanes <= 1, "Different lanes required simultaneous defense");
			AvoidContact(session);
			session.Update(1.0 / 60.0);
			Check(!session.IsGameOver, "Avoiding actual contact caused damage");
		}
		Check(seen.Count == 50, $"First round had {seen.Count} distinct monsters");
		Check(lanes.Count == 3, "Not all three portals were used");
		Check(session.Round >= 3, "Rounds did not continue after clearing 50");
		Check(session.Speed == 180.0f, "Round 3 speed did not increase twice");
		Check(session.Health == 3, "Wrong-lane missed monsters changed HP");
	}

	private static void TestAttackRange()
	{
		GameplaySession outside = WithKind(MonsterKind.Thorn);
		MonsterState monster = outside.Monsters[0];
		outside.MovePlayer(monster.Floor);
		monster.Left = 215.0f;
		outside.RequestAttack();
		outside.Update(0.14);
		Check(!monster.IsDying, "Punch hit outside blue zone");

		GameplaySession wrongLane = WithKind(MonsterKind.Thorn);
		monster = wrongLane.Monsters[0];
		monster.Left = 180.0f;
		int other = (monster.Floor + 1) % 3;
		wrongLane.MovePlayer(other);
		wrongLane.RequestAttack();
		wrongLane.Update(0.2);
		Check(!monster.IsDying, "Punch hit another floor");
	}

	private static void TestDeath()
	{
		GameplaySession session = WithKind(MonsterKind.Thorn);
		MonsterState monster = session.Monsters[0];
		session.MovePlayer(monster.Floor);
		monster.Left = 180.0f;
		session.RequestAttack();
		session.Update(0.14);
		Check(monster.IsDying, "Single punch did not kill thorn");
		float deathLeft = monster.Left;
		session.Update(0.35);
		Check(session.Monsters.Contains(monster), "Monster disappeared before all 8 death frames");
		Check(monster.DeathFrame == 7, "Final death frame not shown");
		Check(monster.Left == deathLeft, "Dead monster continued walking");
		session.Update(0.1);
		Check(!session.Monsters.Contains(monster), "Dead monster did not disappear");
		Check(session.ResolvedThisRound == 1, "Dead monster counted more than once");
	}

	private static void TestSingleHit()
	{
		GameplaySession session = WithKind(MonsterKind.Thorn);
		MonsterState first = session.Monsters[0];
		first.Left = 180.0f;
		MonsterState second = new(MonsterKind.Thorn, first.Floor, 190.0f);
		((List<MonsterState>)session.Monsters).Add(second);
		session.MovePlayer(first.Floor);
		session.RequestAttack();
		session.Update(0.2);
		Check(first.IsDying && !second.IsDying, "A swing did not choose exactly the nearest target");
	}

	private static void TestHeartPunch()
	{
		GameplaySession session = WithKind(MonsterKind.Heart);
		MonsterState monster = session.Monsters[0];
		monster.Left = 180.0f;
		session.MovePlayer(monster.Floor);
		session.RequestAttack();
		session.Update(0.2);
		Check(monster.IsDying && session.Health == 2, "Heart punch must kill heart and cost 1 HP");
		session.Update(0.5);
		Check(session.Health == 2, "Heart punch applied repeated damage");
	}

	private static void TestContact()
	{
		GameplaySession thorn = WithKind(MonsterKind.Thorn);
		MonsterState monster = thorn.Monsters[0];
		monster.Left = 150.0f;
		thorn.MovePlayer(monster.Floor);
		thorn.Update(0.04);
		Check(thorn.Health == 3, "Blue entry alone caused damage");
		thorn.Update(0.06);
		Check(thorn.Health == 2 && monster.IsDying, "Actual body contact did not cause damage");
		thorn.Update(0.1);
		Check(thorn.Health == 2, "Contact damage repeated");

		GameplaySession heart = WithKind(MonsterKind.Heart);
		monster = heart.Monsters[0];
		monster.Left = 136.0f;
		heart.MovePlayer(monster.Floor);
		heart.Update(0.01);
		Check(heart.Health == 4 && monster.IsDying, "Heart contact did not heal above starting HP");
		heart.Update(0.3);
		Check(heart.Health == 4, "Heart healed multiple times");
	}

	private static void TestLargeDelta()
	{
		GameplaySession session = WithKind(MonsterKind.Thorn);
		MonsterState monster = session.Monsters[0];
		monster.Left = 160.0f;
		session.MovePlayer(monster.Floor);
		session.Update(2.0);
		Check(session.Health == 2, "Long frame tunneled through contact");
		session = WithKind(MonsterKind.Thorn);
		monster = session.Monsters[0];
		monster.Left = 180.0f;
		session.MovePlayer(monster.Floor);
		session.RequestAttack();
		session.Update(0.5);
		Check(monster.IsDying && session.Health == 3, "Long frame skipped attack window");
	}

	private static void TestGameOver()
	{
		GameplaySession session = WithKind(MonsterKind.Thorn);
		MonsterState first = session.Monsters[0];
		first.Left = 100.0f;
		session.MovePlayer(first.Floor);
		var monsters = (List<MonsterState>)session.Monsters;
		monsters.Add(new(MonsterKind.Thorn, first.Floor, 100.0f));
		monsters.Add(new(MonsterKind.Thorn, first.Floor, 100.0f));
		session.Update(0.01);
		Check(session.IsGameOver && session.Health == 0, "Zero HP did not end game");
		int spawned = session.SpawnedThisRound;
		session.Update(5.0);
		Check(session.SpawnedThisRound == spawned, "Game-over wave kept spawning");
		session.RequestAttack();
		Check(!session.IsAttacking, "Game-over attack was allowed");
		session.Reset();
		Check(session.Round == 1 && session.Health == 3 && session.PlayerFloor == 0 &&
			session.Monsters.Count == 1 && session.SpawnedThisRound == 1 && session.ResolvedThisRound == 0,
			"Restart did not reset all state");
	}
}
