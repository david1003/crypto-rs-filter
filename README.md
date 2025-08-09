# RS Daily Filter

一個用於計算加密貨幣交易對相對強度（RS）排名並透過 Telegram 發送警示的 .NET 8 控制台應用程式。

## 🎯 專案概述

RS Daily Filter 是一個自動化的加密貨幣分析工具，主要功能包括：

- **相對強度（RS）計算**：使用多時間框架分析計算交易對的相對強度
- **排名系統**：對所有 USDT 永續期貨合約進行 RS 排名
- **五日漸強分析**：識別排除前 N 名後進步最多的潛力標的
- **合約變更監控**：自動檢測新增或移除的交易對
- **Telegram 通知**：自動發送分析結果到指定頻道
- **雙模式運作**：支援即時 API 和歷史檔案模式

## 📊 核心演算法

### RS 計算方法

系統使用四個時間框架進行綜合分析：

1. **當前期（Current Term）**：價格與移動平均線的比率
2. **短期（Short Term）**：線性回歸斜率與移動平均線的比率
3. **中期（Middle Term）**：線性回歸斜率與移動平均線的比率
4. **長期（Long Term）**：線性回歸斜率與移動平均線的比率

### 權重配置

最終強度計算使用可配置的權重：

```
強度分數 = 當前期排名 × 當前期權重 + 短期排名 × 短期權重 + 中期排名 × 中期權重 + 長期排名 × 長期權重
```

## 🏗️ 架構設計

### 設計模式

- **策略模式**：`IExchangeService` 介面支援多種數據來源
- **依賴注入**：使用 Microsoft.Extensions.DependencyInjection
- **服務分離**：RS 分析、通知、交易所服務各自獨立

### 專案結構

```
RSDailyFilter/
├── Common/                 # 共用常數和工具
│   ├── SystemConstants.cs
│   └── Tools.cs
├── Enums/                  # 列舉定義
│   └── ExchangeServiceEnum.cs
├── ExchangeService/        # 交易所服務
│   ├── IExchangeService.cs
│   ├── BinanceExchangeService.cs
│   └── FileExchangeService.cs
├── Helper/                 # 輔助服務
│   └── TelegramHelper.cs
├── Models/                 # 資料模型
│   └── SymbolStrength.cs
├── Services/               # 核心服務
│   ├── IRsAnalysisService.cs
│   ├── RsAnalysisService.cs
│   ├── INotificationService.cs
│   └── NotificationService.cs
├── Extensions/             # 擴展方法
│   └── ServiceCollectionExtensions.cs
└── Program.cs             # 程式進入點
```

## ⚙️ 配置設定

所有設定都在 `appsettings.json` 的 `AppSettings` 區段：

```json
{
  "AppSettings": {
    "FileExchangeTargetDate": "2025-08-09",
    "CurrentTermDays": 7,
    "ShortDays": 5,
    "MiddleDays": 7,
    "LongDays": 10,
    "CurrentTermPercentage": 0.2,
    "ShortTermPercentage": 0.4,
    "MiddleTermPercentage": 0.1,
    "LongTermPercentage": 0.3,
    "RSTakeCount": 30,
    "RSRankDailyResultPath": "J:\\RS Daily Rank Result\\",
    "IgnoreCryptoList": "BTCDOMUSDT,USDCUSDT",
    "TelegramBotToken": "YOUR_BOT_TOKEN",
    "TgRsRankChatId": "YOUR_CHAT_ID",
    "TgContractUpdateChatId": "YOUR_CHAT_ID",
    "SymbolOutputFileName": "UsdtContracts.txt",
    "SymbolPriceFileName": "UsdtPrice.txt",
    "DailyRankedFileName": "DailyRankedResult.txt",
    "ExchangeServiceType": "Binance"
  }
}
```

### 重要配置說明

| 參數 | 說明 | 預設值 |
|------|------|--------|
| `CurrentTermDays` | 當前期天數 | 7 |
| `ShortDays` | 短期天數 | 5 |
| `MiddleDays` | 中期天數 | 7 |
| `LongDays` | 長期天數 | 10 |
| `RSTakeCount` | RS 排名取前幾名 | 30 |
| `ExchangeServiceType` | 服務類型 (Binance/File) | Binance |
| `IgnoreCryptoList` | 忽略的交易對 | BTCDOMUSDT,USDCUSDT |

## 🚀 運作模式

### 1. Binance 模式（即時模式）

```json
{
  "ExchangeServiceType": "Binance"
}
```

- 從 Binance API 獲取即時數據
- 自動檢測合約變更
- 清理過期的歷史資料

### 2. File 模式（測試模式）

```json
{
  "ExchangeServiceType": "File",
  "FileExchangeTargetDate": "2025-08-09"
}
```

- 從指定日期的檔案讀取歷史數據
- 適合測試和回測
- 不執行合約變更檢測和資料清理

## 📁 檔案組織

系統使用日期為基礎的階層結構：

```
RSRankDailyResultPath/
├── 2025-08-08/
│   ├── DailyRankedResult.txt     # 完整排名 JSON
│   ├── UsdtPrice.txt             # 價格數據
│   └── 0.RS_20250808.txt         # TradingView 格式
├── 2025-08-09/
│   ├── DailyRankedResult.txt
│   ├── UsdtPrice.txt
│   └── 0.RS_20250809.txt
└── ...
```

## 🔧 開發環境

### 系統需求

- .NET 8.0 或更高版本
- Windows/Linux/macOS

### 相依套件

- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Http`
- `Newtonsoft.Json`
- `xUnit`（測試專案）
- `Moq`（測試專案）

### 建置與執行

```bash
# 建置專案
dotnet build

# 執行程式
dotnet run

# 執行測試
dotnet test
```

## 🧪 測試

專案包含完整的測試套件：

- **RsAnalysisServiceTests**：RS 分析服務功能測試
- **ConfigurationValidationTests**：配置參數驗證測試
- **覆蓋率**：71 個測試案例，涵蓋主要業務邏輯

```bash
# 執行所有測試
dotnet test

# 執行特定測試類別
dotnet test --filter "ClassName=RsAnalysisServiceTests"
```

## 📱 Telegram 整合

### 設定步驟

1. 建立 Telegram Bot（通過 @BotFather）
2. 取得 Bot Token
3. 將 Bot 加入目標頻道
4. 取得 Chat ID
5. 更新 `appsettings.json` 中的相關設定

### 通知類型

- **合約變更通知**：新增或移除的交易對
- **RS 排名通知**：前 N 名排名結果
- **五日漸強通知**：進步最多的潛力標的

## 🔍 業務邏輯

### RS 排名流程

1. 獲取所有 USDT 永續期貨合約
2. 排除忽略清單中的標的
3. 計算各時間框架的 RS 值
4. 轉換為百分位排名
5. 使用權重計算綜合強度
6. 排序並保存結果

### 五日漸強流程

1. 獲取當前完整排名
2. 排除前 N 名標的
3. 載入 5 天前的排名數據
4. 計算強度進步幅度
5. 返回進步最多的前 5 名

## 🚨 錯誤處理

- 標的處理錯誤不會中斷整體執行
- 歷史數據不足會跳過該標的
- 網路錯誤會記錄並繼續處理
- 檔案操作失敗會優雅處理

## 📝 日誌

系統會輸出詳細的執行日誌：

```
RS Daily Filter Start.
忽略標的: BTCDOMUSDT
忽略標的: USDCUSDT
標的 NEWUSDT 歷史數據不足，跳過處理
檔案已成功傳送到頻道！
已發送RS排名和五日漸強結果。
RS Daily Filter End.
```

## 📄 授權

本專案採用 MIT 授權條款。

## 🆘 常見問題

**Q: 為什麼五日漸強沒有結果？**
A: 確保存在 5 天前的數據資料夾，且 `DailyRankedResult.txt` 檔案完整。

**Q: 如何修改 RS 計算權重？**
A: 修改 `appsettings.json` 中的百分比參數，確保四個權重總和為 1.0。

**Q: 可以自訂忽略的交易對嗎？**
A: 是的，修改 `IgnoreCryptoList` 設定，使用逗號分隔多個標的。

**Q: 如何在測試環境中運行？**
A: 將 `ExchangeServiceType` 設為 "File" 並指定 `FileExchangeTargetDate`。
