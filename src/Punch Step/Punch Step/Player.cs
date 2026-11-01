using System.Windows.Forms;
using Vortice.Mathematics;

// 플레이어의 이미지, 현재 층, 키 입력과 화면 출력을 관리합니다.
class Player : IDisposable
{
	// 1. 기본 자세 이미지: 투명 여백을 제외한 캐릭터 영역입니다.
	private const string StandImagePath = "resource/animation/animation_player/Player_Stand_Right.png";
	private static readonly Rect StandSource = new(23.0f, 16.0f, 84.0f, 62.0f);

	// 2. 위치: X는 고정하고, 현재 층의 발판 높이에 발을 맞춥니다.
	private readonly float _leftX;
	private readonly float[] _floorTopYs;
	private int _floorIndex = 0; // 0부터 차례대로 1층, 2층, 3층입니다.
	private G2Texture? _standImage;

	public Player(float leftX, float[] floorTopYs)
	{
		_leftX = leftX;
		_floorTopYs = floorTopYs;
	}

	public void Initialize()
	{
		_standImage = new G2Texture(StandImagePath);
	}

	// 3. 층 이동: 키를 처음 누른 순간만 처리하므로 한 번에 한 층 이동합니다.
	public void Update(G2InputContext input)
	{
		bool moveUp = input.IsKeyDown(Keys.Up);
		bool moveDown = input.IsKeyDown(Keys.Down);

		// 두 방향을 동시에 누르면 이동하지 않습니다.
		if (moveUp == moveDown)
		{
			return;
		}

		int direction = moveUp ? 1 : -1;
		_floorIndex = Math.Clamp(_floorIndex + direction, 0, _floorTopYs.Length - 1);
	}

	// 4. 기본 자세 출력: 캐릭터 아래쪽이 발판 윗면에 닿도록 배치합니다.
	public void Render()
	{
		float topY = _floorTopYs[_floorIndex] - StandSource.Height;
		Rect destination = new(_leftX, topY, StandSource.Width, StandSource.Height);
		_standImage?.Draw(destination, StandSource);
	}

	// 5. 종료 시 이미지 해제
	public void Dispose()
	{
		_standImage?.Dispose();
		_standImage = null;
	}
}
