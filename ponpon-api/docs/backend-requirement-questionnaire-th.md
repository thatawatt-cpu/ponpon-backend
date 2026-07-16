# PonPon Backend Requirement Questionnaire

เอกสารนี้ใช้สำหรับให้ลูกค้าทดสอบระบบหลังบ้าน/LIFF shop แล้ว feedback requirement กลับมา โดยอิงจาก scope และ module ที่มีอยู่ในระบบ PonPon ปัจจุบัน

โปรเจกต์: PonPon  
ผู้ให้ feedback: ................................................  
บริษัท/ร้านค้า: ................................................  
วันที่ทดสอบ: ................................................  
เวอร์ชันเอกสาร: v0.1

## 1. เป้าหมายการทดสอบ

รบกวนลูกค้าทดลองใช้งาน flow หลักของระบบ แล้วระบุว่าส่วนไหน “ตรงตามที่ต้องการ”, “ต้องปรับ”, หรือ “ยังขาด”

รายการที่อยากให้ทดสอบเป็นพิเศษ:

- [ ] หน้า Dashboard หลังบ้าน
- [ ] การจัดการสินค้าและข้อมูลจาก ZORT
- [ ] การจัดการ banner / home slide
- [ ] Flash sale
- [ ] Coupon / campaign / promotion
- [ ] การสั่งซื้อจาก LIFF shop
- [ ] การชำระเงินผ่าน Omise
- [ ] การจัดส่งผ่าน SHIPPOP
- [ ] การจัดการ order / refund / return
- [ ] ลูกค้า, ที่อยู่, wishlist, recently viewed
- [ ] Review สินค้า
- [ ] Notification / LINE / realtime notification
- [ ] การตั้งค่า integration
- [ ] สิทธิ์ผู้ใช้งานหลังบ้าน

หมายเหตุจากลูกค้า:

..............................................................................

## 2. บทบาทผู้ใช้งานหลังบ้านและสิทธิ์

ระบบมี permission หลักสำหรับ admin ดังนี้:

| Permission | ความหมายโดยประมาณ | ต้องการใช้ไหม | หมายเหตุ |
|---|---|---:|---|
| `dashboard.read` | ดู dashboard | [ ] | |
| `orders.read` | ดู order | [ ] | |
| `orders.manage` | จัดการ order / cancel / return | [ ] | |
| `orders.refund` | อนุมัติ manual refund | [ ] | |
| `products.read` | ดูสินค้า | [ ] | |
| `products.manage` | จัดการสินค้า/รูป/visibility/ตั้งค่า PonPon | [ ] | |
| `customers.read` | ดูลูกค้า | [ ] | |
| `reviews.read` | ดู review | [ ] | |
| `reviews.manage` | approve/reject/delete review | [ ] | |
| `marketing.manage` | จัดการ coupon, campaign, promotion, flash sale | [ ] | |
| `integrations.read` | ดูค่า integration | [ ] | |
| `integrations.manage` | แก้ไข integration / register webhook | [ ] | |
| `settings.manage` | ตั้งค่าระบบ | [ ] | |
| `admin_users.read` | ดู user หลังบ้าน | [ ] | |
| `admin_users.manage` | สร้าง/แก้ไข/reset password user หลังบ้าน | [ ] | |
| `*` | owner ทำได้ทุกอย่าง | [ ] | |

Role ที่ต้องการจริง:

| Role | ทำอะไรได้บ้าง | ใครใช้งาน |
|---|---|---|
| Owner | ................................................................ | ................................................................ |
| Admin | ................................................................ | ................................................................ |
| Staff | ................................................................ | ................................................................ |
| Marketing | ................................................................ | ................................................................ |
| CS / Fulfillment | ................................................................ | ................................................................ |

คำถาม:

- ต้องการจำกัดข้อมูลตามสาขา/คลัง/ทีม หรือทุก admin เห็นข้อมูลเดียวกัน?
- การ reset password ต้องให้ใครทำได้?
- ต้องมี audit log สำหรับการแก้ไข user หลังบ้านระดับไหน?

Feedback:

..............................................................................

## 3. Dashboard หลังบ้าน

ระบบปัจจุบันมี dashboard ที่สรุป:

- ยอดขายรายวัน
- Average order value
- สถานะ order
- สถานะ payment
- สินค้า stock ต่ำ
- order ล่าสุด
- shipping summary
- สถานะ sync จาก ZORT

สิ่งที่ต้องการให้ dashboard แสดง:

- [ ] ยอดขายวันนี้
- [ ] ยอดขายเดือนนี้
- [ ] จำนวน order วันนี้
- [ ] จำนวน order รอชำระเงิน
- [ ] จำนวน order ต้องจัดส่ง
- [ ] จำนวน return/refund
- [ ] สินค้าขายดี
- [ ] สินค้า stock ต่ำ
- [ ] สถานะ ZORT sync
- [ ] สถานะ payment
- [ ] สถานะ shipping
- [ ] อื่น ๆ: ................................................

ต้องการกรอง dashboard ด้วยอะไร:

- [ ] วันที่
- [ ] ช่วงเวลา
- [ ] time zone
- [ ] warehouse
- [ ] sales channel
- [ ] payment method
- [ ] shipping courier

Feedback:

..............................................................................

## 4. สินค้า / Catalog / ZORT Sync

ระบบปัจจุบันใช้ ZORT เป็น source of truth สำหรับสินค้าและ stock และ PonPon เก็บ snapshot/cache เพื่อแสดงใน LIFF shop

ข้อมูลสินค้าที่ระบบรองรับ:

- ชื่อสินค้า
- รายละเอียดสินค้า
- base SKU / variant SKU
- barcode
- ราคา sell price / original price / display price
- stock / available stock / sold count
- category / subcategory จาก ZORT
- variant options เช่น สี/ขนาด
- รูปสินค้า
- น้ำหนัก/กว้าง/ยาว/สูง
- slug
- visibility บน LIFF
- featured / best seller / homepage
- highlights / rich description / promotion badge
- active/inactive จาก ZORT
- product sync status

ฟีเจอร์หลังบ้านที่มี/ควรทดสอบ:

- [ ] ดูรายการสินค้า
- [ ] ค้นหาสินค้า
- [ ] กรองตาม status/source
- [ ] ดูรายละเอียดสินค้า
- [ ] sync สินค้าจาก ZORT
- [ ] sync warehouse จาก ZORT
- [ ] update visibility บน LIFF
- [ ] upload รูปสินค้า
- [ ] จัดเรียง/ตั้ง primary image
- [ ] แก้ PonPon settings เช่น slug, featured, best seller, homepage, rich description

Requirement ที่ต้องยืนยันกับลูกค้า:

| หัวข้อ | คำถาม | คำตอบลูกค้า |
|---|---|---|
| Source of truth | ให้ ZORT เป็นแหล่งข้อมูลหลักของสินค้า/stock ใช่หรือไม่ | |
| แก้สินค้า | หลังบ้าน PonPon ควรแก้ราคา/stock ได้ไหม หรือแก้ที่ ZORT เท่านั้น | |
| สินค้าที่หายจาก ZORT | ให้ซ่อนจาก LIFF อัตโนมัติไหม | |
| Variant | รูปแบบ variant ที่ใช้จริงคืออะไร เช่น สี/ขนาด/รสชาติ | |
| Stock | ต้องกัน stock ตอน add cart, checkout, หรือชำระเงินสำเร็จ | |
| Rich description | ต้องการ editor แบบไหน เช่น plain text, HTML, image upload | |
| Product badge | ต้องการ badge อะไรบ้าง เช่น New, Best Seller, Flash Sale | |

Feedback:

..............................................................................

## 5. Home / Banner / Home Slide

ระบบปัจจุบันมี public home slide และ admin CRUD สำหรับ home slide

ข้อมูลที่ควรยืนยัน:

- [ ] ต้องการ title
- [ ] ต้องการ description
- [ ] ต้องการ badge
- [ ] ต้องการ CTA label
- [ ] ต้องการ link ไปสินค้า/category/external URL
- [ ] ต้องการกำหนด start/end date
- [ ] ต้องการ publish/unpublish
- [ ] ต้องการ drag & drop reorder
- [ ] ต้องการรองรับรูป mobile/desktop แยกกัน

Feedback:

..............................................................................

## 6. Flash Sale

ระบบปัจจุบันรองรับ flash sale และ flash sale quota:

- สร้าง/แก้ไข/ลบ flash sale
- ระบุสินค้าที่ร่วมรายการ
- กำหนด sale price
- กำหนด quantity limit
- reserved quantity
- public endpoint สำหรับ active flash sale
- checkout ใช้ราคาจาก flash sale และกัน quota

Requirement ที่ต้องยืนยัน:

| หัวข้อ | คำถาม | คำตอบลูกค้า |
|---|---|---|
| ช่วงเวลา | Flash sale เริ่ม/จบตาม timezone ใด | |
| สินค้าซ้ำหลายแคมเปญ | ถ้าสินค้าอยู่หลาย flash sale พร้อมกัน ให้เลือกโปรไหน | |
| จำกัดจำนวน | จำกัดต่อ campaign, ต่อสินค้า, หรือต่อลูกค้า | |
| การจอง quota | จองตอน checkout หรือจ่ายเงินสำเร็จ | |
| หมดเวลา payment | ถ้า user ไม่จ่ายในเวลาที่กำหนด คืน quota ตอนไหน | |
| Coupon | coupon ใช้ร่วมกับ flash sale ได้ไหม | |

Feedback:

..............................................................................

## 7. Coupon / Campaign / Promotion

ระบบปัจจุบันรองรับ:

- Coupon CRUD
- Coupon campaign
- Bulk generate coupon
- Coupon usage history
- Coupon audit logs
- Coupon soft delete/deactivate
- Coupon claim สำหรับลูกค้า
- Coupon available/me ใน shop
- Auto promotion / promotion usage
- Scope ระดับ order, product, variant, SKU, category
- Customer scope เช่น new customer, first order, existing customer, customer เฉพาะราย
- Condition เช่น sales channel, shipping channel
- Limit จำนวนใช้ทั้งหมด / ต่อ customer
- minimum subtotal / maximum discount
- fixed / percentage
- combine with flash sale
- multi coupon usage

Checklist ให้ลูกค้า confirm:

- [ ] ต้องใช้ coupon code ที่ลูกค้ากรอกเอง
- [ ] ต้องมี coupon ให้ลูกค้ากดเก็บ
- [ ] ต้องมี coupon ส่วนตัว
- [ ] ต้องมี coupon สำหรับลูกค้าใหม่
- [ ] ต้องมี coupon สำหรับ order แรก
- [ ] ต้องมี campaign dashboard
- [ ] ต้อง bulk generate code จำนวนมาก
- [ ] ต้อง export coupon code
- [ ] ต้องเห็น usage history
- [ ] ต้อง audit log ทุกการแก้ไข coupon
- [ ] ต้องให้ใช้หลาย coupon ใน order เดียว
- [ ] ต้องห้ามใช้กับ flash sale บาง coupon

Business rules ที่ต้องตอบ:

| หัวข้อ | คำตอบลูกค้า |
|---|---|
| ใช้ coupon ได้สูงสุดกี่ใบต่อ order | |
| ถ้ามี coupon หลายใบ ลำดับคำนวณส่วนลดเป็นอย่างไร | |
| Coupon ใช้กับค่าส่งได้ไหม | |
| Coupon ใช้กับสินค้าลดราคา/flash sale ได้ไหม | |
| ลูกค้าขอคืนเงินแล้ว coupon usage ต้องคืนไหม | |
| Coupon ที่หมดอายุแต่ลูกค้ากดเก็บไว้ ควรแสดงไหม | |

Feedback:

..............................................................................

## 8. Order / Checkout

ระบบปัจจุบันรองรับ:

- Pricing preview ก่อนสร้าง order
- Create order จาก LIFF
- คำนวณราคา server-side
- ใช้ quote id
- รองรับ coupon code / coupon codes
- shipping amount/channel
- payment method
- บันทึก pricing snapshot
- sync order จาก ZORT
- ZORT order webhook
- customer ดู order ของตัวเอง
- admin ดู order ทั้งหมด
- cancel order ทั้งฝั่ง customer/admin
- confirm received
- return request
- admin update return request
- approve manual refund
- bulk export order

ข้อมูล checkout ที่ระบบใช้:

- client request id
- quote id
- customer name/email/phone/address
- shipping name/phone/address
- shipping channel
- shipping amount
- coupon code(s)
- payment method
- order description
- product id / variant id / quantity

Flow ที่อยากให้ลูกค้าทดสอบ:

1. เลือกสินค้า/variant
2. ใส่จำนวนสินค้า
3. ใส่ที่อยู่จัดส่ง
4. เลือก shipping channel
5. ใส่ coupon
6. ดู pricing preview
7. สร้าง order
8. ชำระเงิน
9. ตรวจสถานะ order
10. ยกเลิก/คืนสินค้า/ยืนยันรับสินค้า ตาม scenario

Requirement ที่ต้องยืนยัน:

| หัวข้อ | คำถาม | คำตอบลูกค้า |
|---|---|---|
| Order number | ใช้เลขจาก ZORT หรือ PonPon generate เอง | |
| Payment timeout | ให้รอชำระเงินกี่นาที/ชั่วโมง | |
| Cancel by customer | ลูกค้ายกเลิกได้ถึงสถานะไหน | |
| Cancel by admin | Admin ยกเลิกได้ทุกสถานะไหม | |
| Confirm received | ต้อง auto confirm หลังจัดส่งกี่วัน | |
| Return window | ลูกค้าขอคืนสินค้าได้ภายในกี่วัน | |
| Refund | คืนเต็มจำนวน/บางส่วน/ค่าส่ง อย่างไร | |
| Export | export order ต้องมี field อะไรบ้าง | |

Feedback:

..............................................................................

## 9. Payment / Omise

ระบบปัจจุบันรองรับ:

- Omise public config
- PromptPay charge
- Mobile banking charge
- Credit card charge
- Charge status
- Omise webhook
- ผูก charge กับ order

Payment method ที่ต้องการ:

- [ ] PromptPay
- [ ] Mobile banking
- [ ] Credit card
- [ ] COD
- [ ] Bank transfer manual
- [ ] อื่น ๆ: ................................................

Requirement ที่ต้องยืนยัน:

| หัวข้อ | คำถาม | คำตอบลูกค้า |
|---|---|---|
| Payment status | ต้องการสถานะอะไรบ้าง | |
| Failed payment | ถ้าจ่ายไม่สำเร็จให้ retry ได้ไหม | |
| Paid webhook | เมื่อ Omise webhook สำเร็จ ต้อง update ZORT ด้วยไหม | |
| Refund | ทำ refund ผ่าน Omise API หรือ manual | |
| ใบเสร็จ | ต้องออกใบเสร็จ/ใบกำกับภาษีไหม | |

Feedback:

..............................................................................

## 10. Shipping / SHIPPOP

ระบบปัจจุบันรองรับ:

- Check shipping rates
- Create shipping booking
- Get booking by tracking code
- Cancel booking
- Admin sender config
- SHIPPOP webhook
- ข้อมูล parcel: น้ำหนัก กว้าง ยาว สูง
- ข้อมูลผู้รับ: ชื่อ เบอร์ อีเมล ที่อยู่ ตำบล/อำเภอ/จังหวัด/รหัสไปรษณีย์
- courier code / service code
- COD
- label URL

Requirement ที่ต้องยืนยัน:

| หัวข้อ | คำถาม | คำตอบลูกค้า |
|---|---|---|
| Courier | ใช้ขนส่งเจ้าไหนบ้าง | |
| Shipping rate | ให้ลูกค้าเลือกเอง หรือระบบเลือกถูกสุด/เร็วสุด | |
| Sender address | มีผู้ส่งกี่ที่อยู่ | |
| Parcel size | ใช้ขนาดจากสินค้า หรือให้ admin กรอกตอน booking | |
| COD | ต้องรองรับ COD ไหม | |
| Label | ต้อง print label จากหลังบ้านไหม | |
| Tracking | ต้องแสดง tracking ให้ลูกค้าใน LIFF ไหม | |
| Webhook | สถานะจัดส่งใดต้องแจ้งเตือนลูกค้า | |

Feedback:

..............................................................................

## 11. Customer / Address / Engagement

ระบบปัจจุบันรองรับ:

- LINE login สำหรับลูกค้า
- Refresh token / logout / me
- Customer address CRUD
- Set default address
- Admin customer list
- Wishlist
- Recently viewed
- Customer order history
- Customer coupon
- Notification ของลูกค้า

Requirement ที่ต้องยืนยัน:

- [ ] ลูกค้า login ผ่าน LINE เท่านั้น
- [ ] ต้องเก็บ email
- [ ] ต้องเก็บเบอร์โทร
- [ ] ต้องมีหลายที่อยู่
- [ ] ต้องตั้ง default address
- [ ] ต้องแก้ข้อมูล profile ได้
- [ ] ต้องลบบัญชีได้
- [ ] ต้อง export customer ได้
- [ ] ต้องเห็น lifetime value / total orders
- [ ] ต้อง segment ลูกค้าได้

Feedback:

..............................................................................

## 12. Review

ระบบปัจจุบันรองรับ:

- ดู review public ของสินค้า
- Review summary
- ลูกค้า review จาก order item
- แก้/ลบ review
- Upload media สำหรับ review
- Complete media
- ดู media status/file
- Admin review list/detail
- Admin update status
- Admin bulk update status
- Admin delete review

Requirement ที่ต้องยืนยัน:

| หัวข้อ | คำถาม | คำตอบลูกค้า |
|---|---|---|
| Review eligibility | ลูกค้าต้องซื้อสินค้าก่อนถึง review ได้ใช่ไหม | |
| Review moderation | Review ต้อง approve ก่อนแสดงไหม | |
| Media | รองรับรูป/วิดีโอ/กี่ไฟล์/ขนาดเท่าไหร่ | |
| Rating | ใช้ 1-5 ดาวหรือมีหัวข้อย่อย | |
| Incentive | ให้แต้ม/coupon หลัง review ไหม | |
| Edit window | ลูกค้าแก้ review ได้ภายในกี่วัน | |

Feedback:

..............................................................................

## 13. Notification / LINE / Realtime

ระบบปัจจุบันรองรับ:

- Notification list ของ shop
- Unread count
- Mark read / mark all read
- SignalR hub: `/hubs/shop-notifications`
- LINE order notification
- LINE cancellation notifier

Requirement ที่ต้องยืนยัน:

- [ ] แจ้ง order created
- [ ] แจ้ง payment paid
- [ ] แจ้ง payment failed
- [ ] แจ้ง order cancelled
- [ ] แจ้ง shipping booked
- [ ] แจ้ง tracking update
- [ ] แจ้ง return/refund status
- [ ] แจ้ง coupon/campaign
- [ ] แจ้งผ่าน LINE
- [ ] แจ้งใน LIFF notification center
- [ ] แจ้ง admin แบบ realtime
- [ ] แจ้ง email

Feedback:

..............................................................................

## 14. Integration / Settings

ระบบปัจจุบันมี integration settings และรองรับ:

- ZORT
- Omise
- LINE
- Supabase Storage
- SHIPPOP
- Hangfire background jobs
- ZORT webhook register/check

Requirement ที่ต้องยืนยัน:

| Integration | ใช้จริงไหม | ใครเป็นเจ้าของ credential | ต้องแสดง/แก้ในหลังบ้านไหม | หมายเหตุ |
|---|---:|---|---:|---|
| ZORT | [ ] | | [ ] | |
| Omise | [ ] | | [ ] | |
| LINE Login / LIFF | [ ] | | [ ] | |
| LINE Messaging | [ ] | | [ ] | |
| Supabase Storage | [ ] | | [ ] | |
| SHIPPOP | [ ] | | [ ] | |
| Email service | [ ] | | [ ] | |

Feedback:

..............................................................................

## 15. Webhook / Background Job / Sync

ระบบปัจจุบันมี webhook และ background sync หลายจุด:

- ZORT product webhook
- ZORT product add/update/delete/quantity webhook
- ZORT order webhook
- Omise webhook
- SHIPPOP webhook
- Product sync from ZORT
- Order sync from ZORT
- Warehouse sync
- Hangfire durable job

Requirement ที่ต้องยืนยัน:

- [ ] ต้องมีหน้าแสดงประวัติ sync
- [ ] ต้อง retry sync ที่ fail ได้
- [ ] ต้องแจ้งเตือน admin เมื่อ sync fail
- [ ] ต้องเก็บ raw payload ของ webhook
- [ ] ต้องมี audit trail สำหรับ webhook
- [ ] ต้องมี manual re-sync รายสินค้า
- [ ] ต้องมี manual re-sync ราย order

Feedback:

..............................................................................

## 16. รายงาน / Export

ระบบมี dashboard และ order bulk export แล้ว แต่ต้องยืนยันรายงานที่ลูกค้าต้องการจริง

รายงานที่ต้องการ:

- [ ] Sales report
- [ ] Order report
- [ ] Payment report
- [ ] Refund report
- [ ] Return report
- [ ] Shipping report
- [ ] Product sales report
- [ ] Low stock report
- [ ] Coupon usage report
- [ ] Campaign performance report
- [ ] Customer report
- [ ] Review report
- [ ] Sync/Webhook error report

รูปแบบ export:

- [ ] Excel
- [ ] CSV
- [ ] PDF
- [ ] ส่ง email อัตโนมัติ
- [ ] ตั้ง schedule report

Field ที่ต้องมีใน export:

..............................................................................

## 17. Security / Compliance / Audit

สิ่งที่ต้องยืนยัน:

- [ ] Admin login ด้วย email/password
- [ ] Customer login ด้วย LINE
- [ ] Refresh token
- [ ] Role/permission-based access control
- [ ] Audit log สำหรับ admin user
- [ ] Audit log สำหรับ coupon
- [ ] Audit log สำหรับ order action
- [ ] Audit log สำหรับ settings/integration
- [ ] Mask sensitive config values
- [ ] IP allowlist
- [ ] 2FA สำหรับ admin
- [ ] PDPA consent
- [ ] Data retention policy

Feedback:

..............................................................................

## 18. UAT Scenario ที่อยากให้ลูกค้าทดสอบ

ให้ลูกค้าลองทำ scenario เหล่านี้ แล้วใส่ผลลัพธ์/feedback

| Scenario | ผลที่คาดหวัง | ผ่านไหม | Feedback |
|---|---|---:|---|
| Login admin | เข้า dashboard ได้ตามสิทธิ์ | [ ] | |
| Sync product จาก ZORT | สินค้า/stock/variant อัปเดตถูกต้อง | [ ] | |
| ตั้งสินค้าให้แสดงบน LIFF | ลูกค้าเห็นสินค้าใน shop | [ ] | |
| ตั้ง home slide | banner แสดงถูกลำดับ | [ ] | |
| ตั้ง flash sale | ราคาพิเศษและ quota ถูกต้อง | [ ] | |
| สร้าง coupon fixed | ส่วนลดถูกต้อง | [ ] | |
| สร้าง coupon percentage | ส่วนลดและ max discount ถูกต้อง | [ ] | |
| Bulk generate coupon | code ถูกสร้างและผูก campaign | [ ] | |
| ลูกค้า LINE login | profile ถูกสร้าง/อัปเดต | [ ] | |
| ลูกค้าเพิ่มที่อยู่ | address และ default address ถูกต้อง | [ ] | |
| ลูกค้าสร้าง order | order เข้า PonPon/ZORT ถูกต้อง | [ ] | |
| Pricing preview | ราคา, ค่าส่ง, coupon, flash sale ถูกต้อง | [ ] | |
| ชำระ PromptPay | payment status update ถูกต้อง | [ ] | |
| ชำระ card | payment status update ถูกต้อง | [ ] | |
| สร้าง shipping booking | ได้ tracking/label | [ ] | |
| Cancel order | สถานะและ stock/quota/coupon ถูกต้อง | [ ] | |
| Return request | admin/customer เห็นสถานะถูกต้อง | [ ] | |
| Confirm received | order ปิดงานถูกต้อง | [ ] | |
| Review สินค้า | review แสดง/รอ approve ตาม rule | [ ] | |
| Notification | ลูกค้า/admin ได้รับแจ้งเตือน | [ ] | |

## 19. Priority / Phase

ให้ลูกค้าช่วยแบ่ง requirement เพิ่มเติมเป็น phase

### Phase 1: ต้องมีก่อน go-live

..............................................................................

### Phase 2: ทำต่อหลัง go-live

..............................................................................

### Phase 3: Nice to have

..............................................................................

## 20. คำถามเปิดท้าย

1. ถ้าต้องตัด scope เพื่อ go-live ให้ทัน ฟีเจอร์ไหนห้ามตัด?

..............................................................................

2. ฟีเจอร์ไหนยังไม่ชัดและต้อง workshop เพิ่ม?

..............................................................................

3. มีระบบเดิม/ไฟล์ Excel/รายงานตัวอย่าง/ขั้นตอน manual ที่อยากให้ทีมดูเพิ่มไหม?

..............................................................................

4. จุดไหนที่ตอนทดสอบแล้วรู้สึก “ใช้งานยาก” หรือ “ไม่ตรงกับการทำงานจริง”?

..............................................................................

## 21. สรุปสำหรับทีมพัฒนา

ส่วนนี้ทีม dev/PM กรอกหลังได้รับ feedback จากลูกค้า

| หัวข้อ | สรุป |
|---|---|
| Requirement ที่ confirm แล้ว | |
| Requirement ที่ต้องถามเพิ่ม | |
| Scope เพิ่มจากระบบปัจจุบัน | |
| Scope ที่ควรตัด/เลื่อนไป phase ถัดไป | |
| Risk ด้าน integration | |
| Risk ด้านข้อมูล/operation | |
| Impact ต่อ backend | |
| Impact ต่อ frontend/admin | |
| Impact ต่อ database/migration | |
| Estimate เบื้องต้น | |
