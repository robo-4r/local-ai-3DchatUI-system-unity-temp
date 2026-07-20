これは、データの授受のための一時的なレポジトリです。
オリジナルはこちら（https://github.com/masu-katsu/local-ai-3DchatUI-system-unity）です。
キャラクターの上に吹き出しが出るよう修正を加えています。その他微調整あり。

**Unityアプリケーション構成**
```
Assets/
├── Scripts/
│   ├── API/
│   │   ├── ApiClient.cs           # HTTP通信クライアント
│   │   └── ApiModels.cs           # APIデータモデル
│   ├── Chat/
│   │   └── ChatManager.cs         # チャットロジック管理
│   ├── Character/
│   │   ├── CharacterBrain.cs      # キャラクター行動制御
│   │   ├── AnimationController.cs # アニメーション制御
│   │   ├── MovementController.cs  # 移動制御
│   │   └── CharacterStateMachine.cs # 状態管理
│   ├── UI/
│   │   ├── ChatUIManager.cs       # チャットUI管理
│   │   ├── SettingsUIManager.cs    # 設定UI管理
│   │   └── MessageBubble.cs        # メッセージ吹き出し
│   │   └──CharacterBubbleDisplay.cs 		# キャラクター頭上の吹き出し ← **追加**
│   ├── Network/
│   │   └── NetworkMonitor.cs      # ネットワーク監視
│   ├── Core/
│   │   ├── AutonomousPlayBootstrap.cs # シーン自動構築
│   │   └── NavMeshRuntimeBuilder.cs    # NavMesh実行時生成
│   └── Navigation/
│       ├── WaypointManager.cs     # ウェイポイント管理
│       └── Waypoint.cs            # ウェイポイント定義
├── Scenes/
│   └── SampleScene.unity          # メインシーン
└── Prefabs/
    ├── AIMessageBubble.prefab      # AIメッセージ吹き出し
    └── UserMessageBubble.prefab    # ユーザーメッセージ吹き出し
　　└── CharacterBubbleCanvas.prefab		# キャラクター頭上の吹き出し ← **追加**
```

実行時は、X-API-Key設定を適切な値に変更してください
X-API-Key: your-secret-key

---

### 追加Unityパッケージ
- 2D Sprite

---

**最終更新**: 2026年7月20日
