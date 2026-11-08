using Vortice.Mathematics;

// 몬스터는 이미지를 공유하고, 살아 있을 때와 터질 때의 그림만 구분합니다.
sealed class MonsterRenderer : IDisposable
{
	private const string Root = "resource/animation/animation_monster/";
	private readonly G2Texture _thorn = new(Root + "HealthDrain_ThornParasite_160x96.png");
	private readonly G2Texture _heart = new(Root + "Heart_Character_160x96.png");
	private readonly G2Texture _thornPop = new(Root + "HealthDrain_ThornParasite_Pop_8Frame.png");
	private readonly G2Texture _heartPop = new(Root + "Heart_HitPop_Balloon_8Frame_PlayerSize.png");

	public void Render(MonsterState monster, float floorTopY)
	{
		bool isHeart = monster.Kind == MonsterKind.Heart;
		if (monster.IsDying)
		{
			// 원본 시트의 중심 X=80, 발 높이 Y=86을 살아 있는 몬스터 위치에 맞춥니다.
			Rect source = new(monster.DeathFrame * 160.0f, 0.0f, 160.0f, 96.0f);
			Rect destination = new(monster.Left + GameplaySession.MonsterWidth * 0.5f - 80.0f,
				floorTopY - 86.0f, 160.0f, 96.0f);
			(isHeart ? _heartPop : _thornPop).Draw(destination, source);
			return;
		}

		Rect bodySource = isHeart ? new(42.0f, 17.0f, 76.0f, 62.0f) : new(43.0f, 10.0f, 75.0f, 76.0f);
		Rect bodyDestination = new(monster.Left, floorTopY - bodySource.Height,
			GameplaySession.MonsterWidth, bodySource.Height);
		(isHeart ? _heart : _thorn).Draw(bodyDestination, bodySource);
	}

	public void Dispose()
	{
		_heartPop.Dispose();
		_thornPop.Dispose();
		_heart.Dispose();
		_thorn.Dispose();
	}
}
