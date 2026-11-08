using System.Windows.Forms;
using Vortice.Mathematics;

// 플레이어의 이미지, 현재 층, 키 입력과 화면 출력을 관리합니다.
class Player : IDisposable
{
	// 1. 기본 자세 이미지: 투명 여백을 제외한 캐릭터 영역입니다.
	private const string StandImagePath = "resource/animation/animation_player/Player_Stand_Right.png";
	private static readonly Rect StandSource = new(23.0f, 16.0f, 84.0f, 62.0f);
	private const string AttackImagePath = "resource/animation/animation_player/Player_Punch_Right.png";
	private const float AttackFrameWidth = 160.0f;
	private const float AttackFrameHeight = 96.0f;
	// 원본 프레임의 몸 위치와 발 높이를 기준으로 기본 자세에 맞춥니다.
	private const float AttackLeftInset = 23.0f;
	private const float AttackFootY = 79.0f;

	// 2. 위치: X는 고정하고, 현재 층의 발판 높이에 발을 맞춥니다.
	private readonly float _leftX;
	private readonly float[] _floorTopYs;
	private readonly GameplaySession _session;
	private G2Texture? _standImage;
	private G2Texture? _attackImage;

	public Player(float leftX, float[] floorTopYs, GameplaySession session)
	{
		_leftX = leftX;
		_floorTopYs = floorTopYs;
		_session = session;
	}

	public void Initialize()
	{
		_standImage = new G2Texture(StandImagePath);
		_attackImage = new G2Texture(AttackImagePath);
	}

	// 3. 입력은 한 번만 처리하고, 실제 전투 시간은 GameplaySession에서 갱신합니다.
	public void Update(G2InputContext input)
	{
		UpdateFloorMovement(input);
		if (input.IsKeyDown(Keys.Space))
		{
			_session.RequestAttack();
		}
	}

	// 키를 처음 누른 순간만 처리하므로 한 번에 한 층 이동합니다.
	private void UpdateFloorMovement(G2InputContext input)
	{
		bool moveUp = input.IsKeyDown(Keys.Up);
		bool moveDown = input.IsKeyDown(Keys.Down);

		// 두 방향을 동시에 누르면 이동하지 않습니다.
		if (moveUp == moveDown)
		{
			return;
		}

		int direction = moveUp ? 1 : -1;
		_session.MovePlayer(direction);
	}

	// 4. 현재 동작 출력: 공격이 끝나면 기본 자세로 돌아갑니다.
	public void Render()
	{
		if (_session.IsAttacking)
		{
			RenderAttack();
			return;
		}

		float topY = _floorTopYs[_session.PlayerFloor] - StandSource.Height;
		Rect destination = new(_leftX, topY, StandSource.Width, StandSource.Height);
		_standImage?.Draw(destination, StandSource);
	}

	private void RenderAttack()
	{
		int frameIndex = _session.AttackFrame;
		Rect source = new(frameIndex * AttackFrameWidth, 0.0f, AttackFrameWidth, AttackFrameHeight);
		Rect destination = new(
			_leftX - AttackLeftInset, _floorTopYs[_session.PlayerFloor] - AttackFootY,
			AttackFrameWidth, AttackFrameHeight);
		_attackImage?.Draw(destination, source);
	}

	// 5. 종료 시 이미지 해제
	public void Dispose()
	{
		_attackImage?.Dispose();
		_attackImage = null;
		_standImage?.Dispose();
		_standImage = null;
	}
}
