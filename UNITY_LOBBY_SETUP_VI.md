# Setup Menu, Lobby public và Waiting Room

Phần code đã dùng đúng 2 dịch vụ khác nhau:

- **Lobby Service**: tạo/phát hiện phòng public, vào bằng mã Lobby.
- **Relay + Netcode for GameObjects**: kết nối host/client và đồng bộ scene/gameplay.

## 1. Bật Unity Gaming Services

1. Mở project bằng Unity `6000.5.2f1`.
2. Vào **Edit > Project Settings > Services** và link project với một Unity Cloud Project.
3. Trên Unity Dashboard của project, bật **Lobby** và **Relay**.
4. Package cần thiết đã có trong project: Authentication, Multiplayer Services và Netcode for GameObjects.

## 2. NetworkManager trong `Menu_Game`

Tạo object `NetworkManager` nếu scene chưa có, rồi thêm:

- `NetworkManager`
- `Unity Transport`
- Bật **Enable Scene Management** trên `NetworkManager`.
- Thêm các player prefab có `NetworkObject` vào **Network Prefabs**.

Tạo object `OnlineServices`, gắn `NetworkRelayManager`. Object này tự `DontDestroyOnLoad`, vì vậy **chỉ đặt một bản trong `Menu_Game`**.

## 3. Main Menu chỉ có ba nút

Trong Canvas của `Menu_Game`, tạo hierarchy gợi ý:

```text
Canvas
├── MainMenuPanel
│   ├── PlayButton (Chơi)
│   ├── SettingsButton (Setting)
│   └── ExitButton (Exit)
├── PlayPanel
│   ├── TopBar
│   │   ├── PlayerNameInput
│   │   ├── RoomCodeInput
│   │   ├── JoinCodeButton (Nhập mã phòng)
│   │   ├── RoomNameInput
│   │   ├── PublicToggle
│   │   └── CreateButton (Tạo phòng)
│   ├── ScrollView
│   │   └── Viewport/Content
│   ├── RefreshButton
│   ├── BackButton
│   └── StatusText
└── SettingsPanel
```

Gắn `LobbyBrowserUI` lên Canvas (hoặc một object `MenuController`) rồi kéo đầy đủ reference trong Inspector. `PlayPanel` và `SettingsPanel` nên tắt mặc định.

### Prefab một dòng phòng

1. Trong `ScrollView/Viewport/Content`, tạo object `RoomItem` gồm `RoomNameText`, `PlayerCountText`, `JoinButton`.
2. Gắn `RoomListItem`, kéo ba reference tương ứng.
3. Kéo object thành prefab vào `Assets`, xóa bản trong Content.
4. Gán prefab vào `Room Item Prefab` của `LobbyBrowserUI`; gán Content vào `Room List Content`.
5. Content nên có `VerticalLayoutGroup` và `ContentSizeFitter` (Vertical Fit = Preferred Size).

Chỉ phòng bật `PublicToggle` mới xuất hiện trong danh sách. Phòng private vẫn vào được bằng **Lobby Code** hiển thị tại Waiting Room.

## 4. Tạo scene `WaitingRoom`

1. Tạo scene mới và lưu đúng tên `Assets/Scenes/WaitingRoom.unity`.
2. Tạo Canvas gồm: `RoomCodeText`, `PlayerListText`, `CountdownText`, `StartButton`, `LeaveButton`.
3. Tạo object `WaitingRoomController`, thêm `NetworkObject`, sau đó thêm script `WaitingRoomController`.
4. Kéo các UI reference; để `Game Scene Name = House1_Scene`, `Minimum Players = 2`, `Countdown Seconds = 30`.
5. **Không** đặt thêm `NetworkManager` hay `NetworkRelayManager` trong scene này.

`WaitingRoomController` phải là một in-scene `NetworkObject`. Host có nút Start; client không thấy nút này. Khi đủ hai người, countdown tự chạy 30 giây. Nếu người chơi rời khiến còn dưới hai người, countdown tự hủy. Host có thể Start sớm khi đã đủ hai người.

## 5. Build Settings

Vào **File > Build Profiles > Scene List** và xếp:

1. `Menu_Game`
2. `WaitingRoom`
3. `House1_Scene`

Tên trong Inspector phải trùng chính xác (không có `.unity`). Scene `CityScene` có thể giữ lại nếu gameplay đang dùng nó.

## 6. Lưu ý scene gameplay hiện tại

Code cũ `LobbyUIController` là flow lobby nằm chung scene và bắt đầu bằng Enter. Với flow mới, hãy **gỡ/disable component `LobbyUIController`** trong scene để tránh hai controller cùng xử lý nút/phòng.

Trong `House1_Scene`, `GameMatchManager.StartMatchAndAssignRoles()` hiện chưa được tự gọi sau khi đổi scene. Nếu muốn giữ cơ chế chia Dog/Person hiện tại, gọi hàm đó ở server sau khi `House1_Scene` load xong, hoặc chuyển logic spawn sang callback `NetworkManager.SceneManager.OnLoadEventCompleted`.

## 7. Test hai người

1. Build một bản standalone và chạy song song với Unity Editor (không test hai client trong cùng một Editor instance).
2. Host nhập tên, bật/tắt Public rồi tạo phòng.
3. Client A có thể bấm phòng trong danh sách nếu public; hoặc nhập Lobby Code nếu được chủ phòng gửi.
4. Kiểm tra cả hai được chuyển vào `WaitingRoom`.
5. Khi đủ hai người, kiểm tra countdown 30 giây; thử Leave để xác nhận countdown hủy; thử Start từ host.

Nếu danh sách public chưa cập nhật ngay, bấm Refresh. Lobby Service có rate limit nên không nên refresh liên tục mỗi frame.
