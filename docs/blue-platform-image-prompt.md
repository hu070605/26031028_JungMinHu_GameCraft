# 파란 발판 PNG 편집 기록

도구: built-in image_gen.

적용 파일: `resource/background/PunchStep_Platform.png`

원본 백업: `docs/images/PunchStep_Platform_Original_v1.png`

내보내기: 생성된 발판을 기존 코드의 원본 영역 `(33, 413, 1216, 32)`에 맞춰 1280×720 투명 PNG로 정규화했다. 배경과 캐릭터는 게임에서 별도로 그린다. 실제 게임 좌표로 합성한 확인 이미지: `docs/images/PunchStep_BluePlatform_Preview.png`.

## 편집 프롬프트

Edit target: Image 1, the existing single platform transparent PNG sprite. Image 2 is ONLY a style reference for the blue defense pad. Output ONE single horizontal platform sprite, NOT the gameplay scene. Preserve Image 1's 1280x720 transparent canvas, horizontal strip placement and dimensions: opaque artwork restricted approximately to x33..1249, y413..445 (just 32 pixels tall at 57.4% of screen height). Change ONLY the leftmost approximately 150 pixels of the white platform into a short blue defense pad matching image2: cyan top face, cobalt blue front face, dark navy/black cartoon outline, tiny white edge highlight, small dark downward triangle on the front face. The blue pad occupies x33..183 and is exactly aligned with the existing thin platform, same height, no raised bump, no glow extending outside strip. Keep every remaining white tile, crack, black outline and the right end unchanged. No additional platforms, no text, no character, no background sky, no menu. All surrounding empty canvas must have real alpha transparency. Do not crop or center the strip vertically; preserve the original screen-relative placement. Exact requested output size 1280x720 pixels.
