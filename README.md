# CLDV7112 — ABC Retail Azure Storage Solution
## Complete Setup, Run & Deployment Guide

---

## Project Structure

```
AzureStorageApp/
│
├── Controllers/
│   ├── HomeController.cs           ← Dashboard with live counts
│   ├── BlobController.cs           ← Blob Storage image gallery page
│   ├── CustomerController.cs       ← Customer CRUD + Place Order + Order History
│   └── ProductController.cs        ← Product CRUD + Blob + Queue + File Share
│
├── Models/
│   ├── CustomerEntity.cs           ← Azure Table entity + CustomerViewModel
│   ├── ProductEntity.cs            ← Azure Table entity + ProductViewModel
│   └── OrderEntity.cs              ← Azure Table entity for order history
│
├── Services/
│   ├── BlobService.cs              ← Azure Blob Storage (private + SAS tokens)
│   ├── TableService.cs             ← Azure Table Storage (products)
│   ├── CustomerTableService.cs     ← Azure Table Storage (customers)
│   ├── OrderTableService.cs        ← Azure Table Storage (orders)
│   ├── QueueService.cs             ← Azure Queue Storage (order/inventory messages)
│   └── FileService.cs              ← Azure File Share (log files)
│
├── Views/
│   ├── Home/Index.cshtml           ← Dashboard: counts + architecture table
│   ├── Blob/Index.cshtml           ← Image gallery with SAS URLs
│   ├── Customer/
│   │   ├── Index.cshtml            ← Customer list
│   │   ├── Create.cshtml           ← Add customer form
│   │   ├── Edit.cshtml             ← Edit customer form
│   │   ├── Details.cshtml          ← Profile + full order history
│   │   ├── Delete.cshtml           ← Delete confirmation
│   │   └── PlaceOrder.cshtml       ← Product card grid + quantity selector
│   ├── Product/
│   │   ├── Index.cshtml            ← Product list with stock badges + order lock
│   │   ├── Create.cshtml           ← Add product + image upload
│   │   ├── Edit.cshtml             ← Edit product + image replace
│   │   ├── Details.cshtml          ← Product detail view
│   │   ├── Delete.cshtml           ← Delete (blocked if has orders)
│   │   ├── QueueMessages.cshtml    ← View / dequeue messages
│   │   └── Files.cshtml            ← Upload / download / delete log files
│   └── Shared/_Layout.cshtml       ← Master layout + navbar
│
├── appsettings.json                ← ⚠️ Add your connection string here
├── Program.cs
└── AzureStorageApp.csproj
```

---

## STEP 1 — Create an Azure Storage Account

1. Go to https://portal.azure.com → Sign in.
2. Click **"Create a resource"** → Search **"Storage account"** → Click **Create**.
3. Fill in the following:
   - **Resource group**: Create new → `cldv7112-rg`
   - **Storage account name**: e.g. `cldv7112st12345678` (lowercase, no spaces, globally unique)
   - **Region**: South Africa North
   - **Performance**: Standard
   - **Redundancy**: LRS (Locally Redundant Storage)
4. Click **Review + Create** → **Create** → Wait ~30 seconds.

---

## STEP 2 — Get Your Connection String

1. In Azure Portal → go to your new Storage Account.
2. Left sidebar → **Security + networking** → **Access keys**.
3. Click **Show keys**.
4. Copy the full **Connection string** under **key1**.
5. Open `appsettings.json` and replace the placeholder:

```json
"AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "BlobContainerName": "product-images",
    "QueueName": "order-processing",
    "FileShareName": "abc-retail-files"
}
```

> ⚠️ NEVER commit your real connection string to GitHub.
> Before pushing, replace the real value with `YOUR_CONNECTION_STRING_HERE`.

---

## 🛠️ STEP 3 — Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community edition — free)
- [Azure Storage Explorer](https://azure.microsoft.com/products/storage/storage-explorer/) — to visually verify data in Azure

---

## STEP 4 — Run Locally

### Option A — Visual Studio 2022
1. Open `AzureStorageApp.csproj`.
2. Make sure `appsettings.json` has your real connection string.
3. Press **F5** or click the green **Run** button.
4. Browser opens automatically.

### Option B — Command Line
```bash
cd AzureStorageApp
dotnet restore
dotnet run
```
Then open: `https://localhost:5001`

> All Azure resources (tables, blob container, queue, file share) are **created automatically** on first run.

---

## STEP 5 — Add Test Data (Required for Submission)

You need at least **5 records** in each storage service for full marks.

### 1. Table Storage — Customers
- Navbar → **Table Storage → Customers** → **+ Add Customer**
- Add 5+ customers with full details
- Verify: Azure Storage Explorer → Tables → **Customers**

### 2. Table Storage — Products
- Navbar → **Table Storage → Products** → **+ Add Product**
- Add 5+ products, each with an image uploaded
- Verify: Azure Storage Explorer → Tables → **Products**

### 3. Blob Storage — Images
- Images are uploaded automatically when you create a product with an image
- Verify: Navbar → **Blob (Images)** — shows image gallery
- Also verify: Azure Storage Explorer → Blob Containers → **product-images**

### 4. Queue Storage — Order Messages
- Go to Customers → **View & Order** → **Place New Order** for 5+ orders
- Each order sends `[ORDER]` and `[INVENTORY]` messages automatically
- Verify: Navbar → **Queue** — shows all messages colour-coded
- Also verify: Azure Storage Explorer → Queues → **order-processing**

### 5. File Share — Log Files
- Log files are auto-generated for every create, update, delete and order action
- After adding products and placing orders, at least 5 log files will exist
- Verify: Navbar → **File Share** — lists all log files with download option
- Also verify: Azure Storage Explorer → File Shares → **abc-retail-files** → **logs**

### 6. Table Storage — Orders
- Orders are saved automatically when a customer places an order
- Verify: Azure Storage Explorer → Tables → **Orders**
- Also visible: Customer → View & Order → **View** (Details page shows full order history)

---

## STEP 6 — Deploy to Azure App Service

### Option A — Visual Studio 2022 (Recommended — easiest)

1. In Visual Studio, right-click the project in **Solution Explorer** → **Publish**.
2. Click **Add a publish profile**.
3. Select **Azure** → click **Next**.
4. Select **Azure App Service (Windows)** → click **Next**.
5. Sign in to your Azure account if prompted.
6. Click **Create new** and fill in:
   - **Name**: Your student number e.g. `12345678`
   - **Subscription**: Your Azure subscription
   - **Resource group**: `cldv7112-rg` (existing)
   - **Hosting plan**: Click **New** → Name: `cldv7112-plan`, Region: `South Africa North`, Size: **Free (F1)**
7. Click **Create** → wait for it to finish creating (~1 minute).
8. Click **Finish** → then click **Publish**.
9. Wait for the publish to complete — Visual Studio will open your browser automatically.
10. Your URL will be: `https://12345678.azurewebsites.net`

---

## STEP 7 — Add Connection String to Azure App Service

The deployed app needs your storage connection string. Do this **immediately after publishing**:

1. Go to https://portal.azure.com.
2. Search for **App Services** → click your app (e.g. `12345678`).
3. In the left sidebar → click **Configuration**.
4. Under **Application settings** → click **+ New application setting** for each of these:

| Name | Value |
|---|---|
| `AzureStorage__ConnectionString` | Your full connection string from Step 2 |
| `AzureStorage__BlobContainerName` | `product-images` |
| `AzureStorage__QueueName` | `order-processing` |
| `AzureStorage__FileShareName` | `abc-retail-files` |

> ⚠️ Use **double underscore** `__` not `:` — Azure uses `__` to represent nested JSON keys.

5. Click **Save** at the top → click **Continue** on the confirmation popup.
6. Wait ~30 seconds for the app to restart.
7. Browse to your URL — the app should work exactly as it does locally.

---

## 🔧 Azure Storage Services Summary

| Service | Table / Container | Used For |
|---|---|---|
| 📋 Table Storage | `Customers` | Customer profiles (name, email, phone, city) |
| 📋 Table Storage | `Products` | Product data (name, price, qty, category, imageUrl) |
| 📋 Table Storage | `Orders` | Order history per customer |
| 🗂️ Blob Storage | `product-images` | Product images (private, served via SAS tokens) |
| 📨 Queue Storage | `order-processing` | Order + inventory + upload notification messages |
| 📁 File Share | `abc-retail-files/logs` | Auto-generated log files for all operations |

---

## Queue Message Formats

```
[ORDER]     OrderId: ABC123 | Customer: John Smith | Product: Keyboard | Qty: 2 | Status: Processing
[INVENTORY] Product: Wireless Mouse | NewQty: 50 | Status: Updated
[INVENTORY] Product REMOVED: OldItem | ID: abc-123
[UPLOAD]    Image uploaded: product-photo.jpg
[CUSTOMER]  New customer registered: Jane Doe | Email: jane@abc.com | ID: xyz-456
```

---

## Key Business Rules

- **Products with orders cannot be deleted** — the delete button is locked (🔒) on any product that has at least one order. This protects order history integrity.
- **Customers can always be deleted** — customer records are independent.
- **Stock is deducted automatically** when an order is placed — the product quantity updates in real time.
- **Out-of-stock products** are hidden from the Place Order screen automatically.
- **Images** are stored in a private blob container and served via time-limited SAS tokens (no public access required on the storage account).

---

## NuGet Packages

```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="Azure.Storage.Queues" Version="12.17.1" />
<PackageReference Include="Azure.Data.Tables" Version="12.8.3" />
<PackageReference Include="Azure.Storage.Files.Shares" Version="12.17.1" />
<PackageReference Include="Microsoft.Extensions.Azure" Version="1.7.3" />
```

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `AuthenticationFailed` on startup | Re-copy connection string from Portal → Storage Account → Access keys |
| Images not showing | Check `BlobService.cs` uses `PublicAccessType.None` (private) — SAS tokens handle display |
| App crashes on startup locally | `appsettings.json` connection string is still the placeholder value |
| App crashes on Azure but works locally | App Service configuration settings missing or using `:` instead of `__` |
| Edit saves nothing silently | Old bug — fixed. Ensure you're using the latest code with hidden `PartitionKey` fields |
| Queue shows 0 messages | Create products and place orders first — messages are auto-generated |
| Delete button is locked 🔒 | That product has existing orders — this is by design to protect order history |
| 404 after deployment | App Service name in URL must match exactly what you created in Step 6 |

---

## Submission Checklist

- [ ] Student number in document filename and header
- [ ] Module code: CLDV7112
- [ ] 5+ customer records in Table Storage (screenshot from app + Storage Explorer)
- [ ] 5+ product records in Table Storage with images (screenshot)
- [ ] 5+ blobs in Blob container (screenshot from Blob gallery page + Storage Explorer)
- [ ] 5+ messages in Queue (screenshot)
- [ ] 5+ log files in File Share (screenshot)
- [ ] 5+ order records in Orders table (screenshot)
- [ ] App deployed to Azure App Service (screenshot of publish process)
- [ ] Deployed URL accessible in browser (screenshot): `https://YourStudentNumber.azurewebsites.net`
- [ ] GitHub repository link included

---

*CLDV7112 | IIE | Cloud Development | ABC Retail Azure Storage Project — Part 1*
