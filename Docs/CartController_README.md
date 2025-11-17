# CartController - Qu?n lý Gi? Hàng

## T?ng quan
H? th?ng qu?n lý gi? hàng ?ã ???c tri?n khai v?i ki?n trúc tách bi?t gi?a Controller và Service, giúp code d? b?o trì và m? r?ng.

## C?u trúc d? án

### 1. CartService (`Services\CartService.cs`)
D?ch v? qu?n lý logic nghi?p v? c?a gi? hàng, bao g?m:

**Các ph??ng th?c chính:**
- `GetCart()`: L?y gi? hàng hi?n t?i t? Session
- `AddItem()`: Thêm s?n ph?m vào gi? hàng (2 overload)
  - Thêm theo thông tin chi ti?t (productId, productName, unitPrice, quantity, image)
  - Thêm t? ??i t??ng Product
- `RemoveItem(int productId)`: Xóa s?n ph?m kh?i gi? hàng
- `UpdateQuantity(int productId, int quantity)`: C?p nh?t s? l??ng s?n ph?m
- `ClearCart()`: Xóa toàn b? gi? hàng
- `GetTotalItems()`: L?y t?ng s? l??ng s?n ph?m trong gi?
- `GetTotalAmount()`: L?y t?ng giá tr? gi? hàng
- `IsProductInCart(int productId)`: Ki?m tra s?n ph?m có trong gi? không
- `GetProductQuantity(int productId)`: L?y s? l??ng c?a m?t s?n ph?m

**Model CartItem:**
```csharp
public class CartItem
{
    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string Image { get; set; }
    public decimal TotalPrice { get; } // Property tính toán
}
```

### 2. CartController (`Controllers\CartController.cs`)
Controller x? lý các yêu c?u liên quan ??n gi? hàng:

**Action Methods:**

#### GET: Cart/Index
- **M?c ?ích**: Hi?n th? gi? hàng
- **View**: `Views\Cart\Index.cshtml`
- **Ch?c n?ng**: 
  - Hi?n th? danh sách s?n ph?m trong gi?
  - Hi?n th? s?n ph?m g?i ý d?a trên danh m?c
  - Tính t?ng ti?n

#### POST: Cart/AddItem
- **Tham s?**: `productId`, `quantity` (m?c ??nh = 1)
- **Ch?c n?ng**: Thêm s?n ph?m vào gi? hàng
- **Validation**: Ki?m tra s? l??ng > 0, s?n ph?m t?n t?i
- **Response**: Redirect v? trang tr??c ho?c Cart/Index

#### POST: Cart/RemoveItem
- **Tham s?**: `productId`
- **Ch?c n?ng**: Xóa s?n ph?m kh?i gi? hàng
- **Response**: Redirect v? Cart/Index

#### POST: Cart/UpdateQuantity
- **Tham s?**: `productId`, `quantity`
- **Ch?c n?ng**: C?p nh?t s? l??ng s?n ph?m
- **Validation**: Ki?m tra s? l??ng > 0
- **Response**: Redirect v? Cart/Index

#### POST: Cart/ClearCart
- **Ch?c n?ng**: Xóa toàn b? gi? hàng
- **Response**: Redirect v? Cart/Index

#### GET: Cart/GetCartCount (AJAX)
- **Ch?c n?ng**: L?y s? l??ng và t?ng ti?n trong gi? (dùng cho AJAX)
- **Response**: JSON `{ count, amount }`

### 3. View - Cart/Index (`Views\Cart\Index.cshtml`)

**Tính n?ng giao di?n:**
- Hi?n th? danh sách s?n ph?m trong gi? v?i hình ?nh
- ?i?u ch?nh s? l??ng tr?c ti?p (nút +/-)
- Xóa t?ng s?n ph?m
- Xóa toàn b? gi? hàng
- Hi?n th? t?ng ti?n, phí v?n chuy?n
- Thông báo alert t? ??ng ?óng sau 5 giây
- Hi?n th? s?n ph?m g?i ý khi gi? hàng tr?ng ho?c có s?n ph?m
- Responsive design v?i Bootstrap

**Các section:**
1. **Danh sách s?n ph?m**: Table hi?n th? s?n ph?m v?i các thao tác
2. **Thông tin ??n hàng**: Card sticky hi?n th? tóm t?t ??n hàng
3. **S?n ph?m g?i ý**: Partial view hi?n th? s?n ph?m liên quan

### 4. Tích h?p v?i các Controller khác

#### HomeController
- Các action Cart-related ?ã ???c chuy?n h??ng ??n CartController
- Method `GetCartService()` ?? s? d?ng CartService
- ?ã xóa class CartItem c? (chuy?n sang Services namespace)

#### ProductDetail View
- S? d?ng form POST ??n `Cart/AddItem`
- ??ng b? s? l??ng gi?a input và hidden field
- Nút "Thêm vào gi? hàng" và "Mua ngay"

#### _Layout.cshtml
- Icon gi? hàng v?i badge hi?n th? s? l??ng
- Link ??n `Cart/Index`
- S? d?ng `_22DH114699_LTW.Services.CartItem`

## Cách s? d?ng

### 1. Thêm s?n ph?m vào gi? hàng
```csharp
// T? Controller
var cartService = new CartService(Session);
cartService.AddItem(product, quantity);
```

```html
<!-- T? View -->
@using (Html.BeginForm("AddItem", "Cart", FormMethod.Post))
{
    @Html.AntiForgeryToken()
    <input type="hidden" name="productId" value="@Model.ProductID" />
    <input type="number" name="quantity" value="1" />
    <button type="submit">Thêm vào gi?</button>
}
```

### 2. Xem gi? hàng
```
URL: /Cart ho?c /Cart/Index
```

### 3. Xóa s?n ph?m
```html
@using (Html.BeginForm("RemoveItem", "Cart", FormMethod.Post))
{
    @Html.AntiForgeryToken()
    <input type="hidden" name="productId" value="@item.ProductID" />
    <button type="submit">Xóa</button>
}
```

### 4. C?p nh?t s? l??ng
```html
@using (Html.BeginForm("UpdateQuantity", "Cart", FormMethod.Post))
{
    @Html.AntiForgeryToken()
    <input type="hidden" name="productId" value="@item.ProductID" />
    <input type="number" name="quantity" value="@item.Quantity" />
    <button type="submit">C?p nh?t</button>
}
```

### 5. Xóa toàn b? gi? hàng
```html
@using (Html.BeginForm("ClearCart", "Cart", FormMethod.Post))
{
    @Html.AntiForgeryToken()
    <button type="submit">Xóa t?t c?</button>
}
```

## L?u tr? d? li?u
- Gi? hàng ???c l?u trong **Session** v?i key `"CART"`
- Ki?u d? li?u: `List<CartItem>`
- Session t? ??ng expire khi ?óng browser ho?c timeout

## Tính n?ng n?i b?t

? **Ki?n trúc phân tách**: Service layer riêng bi?t, d? test và maintain  
? **Validation ??y ??**: Ki?m tra s? l??ng, s?n ph?m t?n t?i  
? **UX t?t**: Alert t? ??ng ?óng, ?i?u ch?nh s? l??ng tr?c quan  
? **Responsive**: Giao di?n t??ng thích mobile  
? **S?n ph?m g?i ý**: T?ng kh? n?ng cross-sell  
? **Anti-forgery token**: B?o m?t form submit  

## Các c?i ti?n có th? th?c hi?n

?? **L?u gi? hàng vào Database**: ?? gi? hàng không m?t khi ?óng browser  
?? **AJAX operations**: C?p nh?t gi? hàng không reload trang  
?? **Stock management**: Ki?m tra t?n kho tr??c khi thêm vào gi?  
?? **Voucher/Discount**: Tính n?ng mã gi?m giá  
?? **Save for later**: L?u s?n ph?m ?? mua sau  
?? **Cart expiration**: T? ??ng xóa gi? hàng sau th?i gian nh?t ??nh  

## Ki?m th?

### Test Cases c? b?n:
1. ? Thêm s?n ph?m m?i vào gi? hàng tr?ng
2. ? Thêm s?n ph?m ?ã có trong gi? (t?ng s? l??ng)
3. ? C?p nh?t s? l??ng s?n ph?m
4. ? Xóa s?n ph?m kh?i gi?
5. ? Xóa toàn b? gi? hàng
6. ? Hi?n th? gi? hàng tr?ng
7. ? Tính t?ng ti?n chính xác

## Liên h? / H? tr?
- Developer: 22DH114699
- Repository: https://github.com/qangnh/22DH114699_LTW
