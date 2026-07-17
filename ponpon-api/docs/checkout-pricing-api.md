# Checkout pricing integration

Call `POST /api/orders/pricing-preview` whenever cart items, shipping address/channel, or coupon code changes. Use the returned amounts as the checkout display only; `POST /api/orders` validates the quote and atomically reserves coupon, flash-sale quota, and stock.

```json
{
  "customerEmail": "buyer@example.com",
  "shippingName": "Buyer",
  "shippingPhone": "0812345678",
  "shippingAddress": "99 Road district state province 10110",
  "shippingChannel": "COURIER_CODE_FROM_RATES",
  "couponCode": "WELCOME10",
  "items": [
    { "productId": "00000000-0000-0000-0000-000000000000", "variantId": null, "quantity": 1 }
  ]
}
```

Partial cart quote before the customer has a shipping address:

```json
{
  "customerEmail": null,
  "shippingName": null,
  "shippingPhone": null,
  "shippingAddress": null,
  "shippingChannel": null,
  "couponCode": null,
  "items": [
    { "productId": "00000000-0000-0000-0000-000000000000", "variantId": null, "quantity": 1 }
  ]
}
```

When shipping details are missing, the response has `isFinal: false`,
`shippingFinalized: false`, and `calculationStatus: "partial"`. The response can be
shown in cart/checkout as an estimated item total, but it cannot be used to create an
order until the customer adds a valid shipping name, phone, address, and channel and a
new final quote is created.

`shippingChannel` must be an actual SHIPPOP `courierCode` or `serviceCode` returned by
`POST /api/shipping/rates`. Do not send placeholder values such as `"standard"`. If the
customer has a shipping address but has not selected a courier yet, send
`shippingChannel: null`; the pricing API will choose the default standard channel and
return it as `selectedShippingChannel`. The frontend must send that selected channel when
creating the order. If shipping details are missing, `selectedShippingChannel` is `null`
and the quote remains partial.

Shipping options:

`POST /api/shipping/rates` returns only the customer-facing choices: standard and fastest.
The standard option is always `isDefault: true`. If the same courier is both standard and
fastest, the API returns one option with `optionType: "standard_fastest"`.

```json
[
  {
    "courierCode": "FLASH",
    "courierName": "Flash Express",
    "serviceName": "Flash Express",
    "serviceCode": "FLASH",
    "price": 45,
    "estimateTime": "2-3 วัน",
    "optionType": "standard",
    "label": "ปานกลาง",
    "isDefault": true,
    "estimatedMinDays": 2,
    "estimatedMaxDays": 3
  },
  {
    "courierCode": "KERRY",
    "courierName": "Kerry Express",
    "serviceName": "Kerry Express",
    "serviceCode": "KERRY",
    "price": 65,
    "estimateTime": "1 วัน",
    "optionType": "fastest",
    "label": "เร็วที่สุด",
    "isDefault": false,
    "estimatedMinDays": 1,
    "estimatedMaxDays": 1
  }
]
```

Render `lines`, `itemSubtotal`, `shippingAmount`, `couponDiscountAmount`, `vatAmount`, `grandTotal`, and `adjustments`. Treat HTTP 400 as an invalid coupon, unavailable quota/stock, unsupported shipping address, or invalid request and show the API error message.

Admin endpoints:

- Coupon CRUD: `/api/admin/coupons`
- Coupon usage history: `GET /api/admin/coupons/{id}/usages`
- Flash-sale quota: `quantityLimit` and `reservedQuantity` on `/api/admin/flash-sales`
- Immutable order calculation: `GET /api/admin/orders/{id}/pricing-snapshot`

Coupon product scope:

- `scopes: []` or omitted = coupon applies to the whole order.
- `type: "product"` requires `productId`.
- `type: "variant"` requires `variantId` or `sku`.
- `type: "sku"` requires `sku`.
- `type: "category"` requires `categoryName`; category scope matches both product category and sub-category.

```json
{
  "code": "TEA10",
  "type": "percentage",
  "value": 10,
  "minimumSubtotal": 0,
  "maximumDiscount": null,
  "startsAtUtc": null,
  "endsAtUtc": null,
  "canCombineWithFlashSale": true,
  "maximumTotalUses": 100,
  "maximumUsesPerCustomer": 1,
  "isActive": true,
  "scopes": [
    { "type": "product", "productId": "00000000-0000-0000-0000-000000000000" },
    { "type": "sku", "sku": "TEA-RED" },
    { "type": "category", "categoryName": "Tea" }
  ]
}
```

When scopes are present, the coupon discount is calculated from only the matching cart lines. If no line matches, pricing preview and checkout return HTTP 400.

Coupon customer scope:

- `customerScopes: []` or omitted = coupon applies to all customers.
- `type: "new_customer"` = customer has no previous paid, non-voided orders.
- `type: "first_order"` = same eligibility as `new_customer` at checkout time.
- `type: "existing_customer"` = customer has at least one previous paid, non-voided order.
- `type: "customer"` requires `customerId`; this is for customer-specific/private coupons.

```json
{
  "code": "VIP100",
  "type": "fixed",
  "value": 100,
  "minimumSubtotal": 500,
  "maximumDiscount": null,
  "startsAtUtc": null,
  "endsAtUtc": null,
  "canCombineWithFlashSale": true,
  "maximumTotalUses": 100,
  "maximumUsesPerCustomer": 1,
  "isActive": true,
  "scopes": [],
  "customerScopes": [
    { "type": "existing_customer" },
    { "type": "customer", "customerId": "00000000-0000-0000-0000-000000000000" }
  ]
}
```

If multiple customer scopes are supplied, a customer can use the coupon when any one scope matches.

Coupon checkout conditions:

- `conditions: []` or omitted = no sales channel or shipping restriction.
- `sales_channel` values use the order sales channel; customer checkout currently sends `LineLiff`.
- Payment method is not part of pricing calculation yet.
- `shipping_channel` values use the shipping courier/service code sent in `shippingChannel`.
- Values within the same condition type are OR. Different condition types are AND.

```json
{
  "conditions": [
    { "type": "sales_channel", "value": "LineLiff" },
    { "type": "shipping_channel", "value": "flash" },
    { "type": "shipping_channel", "value": "kerry" }
  ]
}
```

Send the selected `paymentMethod` only when creating the order or creating the payment. Pricing
preview does not calculate by payment method yet.

Bulk generate coupons:

`POST /api/admin/coupons/bulk-generate`

```json
{
  "prefix": "VIP",
  "count": 100,
  "codeLength": 8,
  "campaignId": "00000000-0000-0000-0000-000000000000",
  "template": {
    "type": "fixed",
    "value": 100,
    "minimumSubtotal": 500,
    "maximumDiscount": null,
    "startsAtUtc": null,
    "endsAtUtc": null,
    "canCombineWithFlashSale": true,
    "maximumTotalUses": 1,
    "maximumUsesPerCustomer": 1,
    "isActive": true,
    "scopes": [],
    "customerScopes": [],
    "conditions": []
  }
}
```

Response:

```json
{
  "batchId": "00000000-0000-0000-0000-000000000000",
  "campaignId": "00000000-0000-0000-0000-000000000000",
  "createdCount": 100,
  "codes": ["VIP-8K2P9Q4M"]
}
```

Audit logs:

- `GET /api/admin/coupons/{id}/audit-logs`
- Logged actions: `created`, `updated`, `deleted`, `deactivated`, `bulk_generated`.
- Each log includes `actorUserId`, `actorUserType`, `beforeJson`, `afterJson`, and optional `batchId`.

Coupon campaigns:

- CRUD: `/api/admin/coupon-campaigns`
- Filter coupons: `GET /api/admin/coupons?campaignId={campaignId}`
- A campaign can contain multiple bulk-generate batches. `campaignId` groups the campaign,
  while `batchId` identifies each generation run.
- Campaign responses include `generatedCoupons`, `redeemedCoupons`, `remainingCoupons`,
  `activeUsageCount`, and `totalDiscountAmount`.
- Deleting a campaign that already has coupons deactivates it and preserves reporting history.
- An inactive, not-yet-started, or expired campaign makes its coupons unusable.

```json
{
  "name": "8.8 Campaign",
  "description": "Coupons for the 8.8 sale",
  "startsAtUtc": "2026-08-07T17:00:00Z",
  "endsAtUtc": "2026-08-08T16:59:59Z",
  "isActive": true
}
```
