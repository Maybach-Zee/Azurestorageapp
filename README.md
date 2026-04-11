# CLDV7112 — ABC Retail Azure Storage Solution
## Complete Setup, Run & Deployment Guide

---

## 📁 Project Structure

```
AzureStorageApp/
│
├── Controllers/
│   ├── HomeController.cs           ← Dashboard (counts from all services)
│   ├── CustomerController.cs       ← Customer CRUD + Place Order
│   └── ProductController.cs        ← Product CRUD + Queue + File + Blob
│
├── Models/
│   ├── CustomerEntity.cs           ← Table entity + ViewModel for customers
│   └── ProductEntity.cs            ← Table entity + ViewModel for products
│
├── Services/
│   ├── BlobService.cs              ← Azure Blob Storage (product images)
│   ├── TableService.cs             ← Azure Table Storage (products)
│   ├── CustomerTableService.cs     ← Azure Table Storage (customers)
│   ├── QueueService.cs             ← Azure Queue Storage (orders/inventory)
│   └── FileService.cs              ← Azure File Share (log files)
│
├── Views/
│   ├── Home/Index.cshtml           ← Dashboard with stats & architecture table
│   ├── Customer/                   ← Index, Create, Edit, Details, Delete, PlaceOrder
│   ├── Product/                    ← Index, Create, Edit, Details, Delete
│   │                                  QueueMessages, Files
│   └── Shared/_Layout.cshtml       ← Master layout with navbar
│
├── appsettings.json                ← ⚠️ Add your connection string here
├── Program.cs
└── AzureStorageApp.csproj
```

---

## ☁️ STEP 1 — Create an Azure Storage Account

1. Go to https://portal.azure.com → Sign in.
2. Click **"Create a resource"** → Search **"Storage account"** → Click **Create**.
3. Fill in:
   - **Resource group**: Create new → `cldv7112-rg`
   - **Storage account name**: `cldv7112YourStudentNumber` (lowercase, no spaces, globally unique)
   - **Region**: South Africa North
   - **Performance**: Standard
   - **Redundancy**: LRS
4. Click **Review + Create** → **Create** → Wait ~30 seconds.

---

## 🔑 STEP 2 — Get Your Connection String

1. In Azure Portal → Your Storage Account.
2. Left sidebar → **Security + networking** → **Access keys**.
3. Click **Show keys**.
4. Copy **Connection string** under **key1**.
5. Open `appsettings.json`, replace the placeholder:

```json
"AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "BlobContainerName": "product-images",
    "QueueName": "order-processing",
    "FileShareName": "abc-retail-files"
}
```

> ⚠️ NEVER commit your real connection string to GitHub.

---

## 🛠️ STEP 3 — Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community — free)
- [Azure Storage Explorer](https://azure.microsoft.com/products/storage/storage-explorer/) ← to verify data visually

---

## ▶️ STEP 4 — Run Locally

### Visual Studio 2022
1. Open `AzureStorageApp.csproj`.
2. Press **F5**.

### Command Line
```bash
cd AzureStorageApp
dotnet restore
dotnet run
```

Browse to: `https://localhost:5001`

All Azure containers, tables, queues, and file shares are **created automatically** on first run.

---

## ✅ STEP 5 — Test All 4 Azure Storage Services

### 1. 📋 Table Storage — Customers
- Navbar → **Table Storage → Customers** → Add 5+ customers.
- Verify in Azure Storage Explorer → Tables → **Customers**.

### 2. 📋 Table Storage — Products
- Navbar → **Table Storage → Products** → Add 5+ products (with images).
- Verify in Azure Storage Explorer → Tables → **Products**.

### 3. 🗂️ Blob Storage
- When adding a product, upload an image.
- Verify in Azure Storage Explorer → Blob Containers → **product-images**.

### 4. 📨 Queue Storage
- Click **Queue** in navbar to see all queued messages (colour coded).
- Place orders via Customers → 🛒 Order to add `[ORDER]` messages.
- Verify in Azure Storage Explorer → Queues → **order-processing**.

### 5. 📁 File Share
- Click **File Share** in navbar.
- Log files auto-appear after every create/update/delete/order action.
- Verify in Azure Storage Explorer → File Shares → **abc-retail-files** → **logs**.

---

## 🚀 STEP 6 — Deploy to Azure App Service

### Option A — Visual Studio (Easiest)
1. Right-click project in Solution Explorer → **Publish**.
2. Select **Azure** → **Azure App Service (Windows)** → Next.
3. Click **Create new**:
   - **App name**: `StudentNumber` (e.g. `12345678`)
   - **Resource group**: `cldv7112-rg`
   - **Hosting plan**: Free (F1) tier
4. Click **Create** → then **Publish**.
5. Your app URL will be: `https://StudentNumber.azurewebsites.net`

### Option B — Azure CLI
```bash
# Login
az login

# Create App Service plan (free tier)
az appservice plan create --name cldv7112-plan --resource-group cldv7112-rg --sku FREE --is-linux

# Create the web app
az webapp create --name YourStudentNumber --resource-group cldv7112-rg --plan cldv7112-plan --runtime "DOTNET|8.0"

# Deploy from publish folder
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
az webapp deployment source config-zip --resource-group cldv7112-rg --name YourStudentNumber --src ../deploy.zip
```

### Option C — GitHub Actions (Automated)
1. Push code to GitHub.
2. In Azure Portal → App Service → **Deployment Center**.
3. Select **GitHub** → Authorize → Select your repo → Save.
4. Every push to main will auto-deploy.

---

## ⚙️ STEP 7 — Configure Connection String on Azure App Service

After deploying, you must add your connection string to the App Service:

1. Azure Portal → App Service → **Configuration** → **Application settings**.
2. Click **+ New application setting**:
   - **Name**: `AzureStorage__ConnectionString`
   - **Value**: Your full connection string
3. Add settings for:
   - `AzureStorage__BlobContainerName` = `product-images`
   - `AzureStorage__QueueName` = `order-processing`
   - `AzureStorage__FileShareName` = `abc-retail-files`
4. Click **Save** → **Continue**.

> Note: Double underscore `__` is used instead of `:` for nested settings in Azure App Service.

---

## 🔧 Azure Storage Services Summary

| Service | Used For | Azure Resource |
|---|---|---|
| 📋 Table Storage | Customer profiles | Table: `Customers` |
| 📋 Table Storage | Product data (name, price, qty, imageUrl) | Table: `Products` |
| 🗂️ Blob Storage | Product images (public URLs) | Container: `product-images` |
| 📨 Queue Storage | Orders, inventory, upload notifications | Queue: `order-processing` |
| 📁 File Share | Log files (auto + manual upload) | Share: `abc-retail-files/logs` |

---

## 📝 Queue Message Formats

```
[ORDER]     OrderId: ABC123 | Customer: John Smith | Product: Keyboard | Qty: 2 | Status: Processing
[INVENTORY] Product: Wireless Mouse | NewQty: 50 | Status: Updated
[INVENTORY] Product REMOVED: OldItem | ID: abc-123
[UPLOAD]    Image uploaded: product-photo.jpg
[CUSTOMER]  New customer registered: Jane Doe | Email: jane@abc.com | ID: xyz-456
```

---

## 📌 NuGet Packages

```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="Azure.Storage.Queues" Version="12.17.1" />
<PackageReference Include="Azure.Data.Tables" Version="12.8.3" />
<PackageReference Include="Azure.Storage.Files.Shares" Version="12.17.1" />
<PackageReference Include="Microsoft.Extensions.Azure" Version="1.7.3" />
```

---

## 🚨 Troubleshooting

| Problem | Fix |
|---|---|
| `AuthenticationFailed` | Re-copy connection string from Azure Portal → Access keys |
| Images not displaying | Set Blob container access to **Blob (anonymous read)** in Portal |
| App crashes on startup | Check `appsettings.json` has a valid connection string |
| 404 on deployed app | Ensure App Service application settings use `__` not `:` |
| Queue shows 0 messages | Add products/customers first — messages generate automatically |

---

## 📋 Submission Checklist

- [ ] 5+ customer records in Table Storage (screenshot)
- [ ] 5+ product records in Table Storage with images (screenshot)
- [ ] 5+ blobs visible in Blob container (screenshot)
- [ ] 5+ messages in Queue (screenshot)
- [ ] 5+ log files in File Share (screenshot)
- [ ] App deployed to Azure App Service (screenshot of deployment)
- [ ] URL accessible: `https://YourStudentNumber.azurewebsites.net`
- [ ] GitHub repository link included in submission doc

---

*CLDV7112 | IIE | Cloud Development | ABC Retail Azure Storage Project — Part 1*
