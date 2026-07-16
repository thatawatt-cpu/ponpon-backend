import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "C:/Work/ponpon-backend/outputs/backend-requirement-questionnaire";
const outputPath = `${outputDir}/ponpon-backend-requirement-questionnaire-th.xlsx`;

const theme = {
  navy: "#1E3A5F",
  blue: "#2563EB",
  teal: "#0F766E",
  green: "#16A34A",
  amber: "#F59E0B",
  red: "#DC2626",
  purple: "#7C3AED",
  gray900: "#111827",
  gray700: "#374151",
  gray100: "#F3F4F6",
  gray50: "#F9FAFB",
  border: "#D1D5DB",
  white: "#FFFFFF",
};

const workbook = Workbook.create();

function setWidth(sheet, widths) {
  widths.forEach((width, idx) => {
    sheet.getCell(0, idx).format.columnWidth = width;
  });
}

function title(sheet, titleText, subtitle) {
  sheet.showGridLines = false;
  sheet.getRange("A1:H1").merge();
  sheet.getRange("A1").values = [[titleText]];
  sheet.getRange("A1").format = {
    fill: theme.navy,
    font: { bold: true, color: theme.white, size: 16 },
    wrapText: true,
  };
  sheet.getRange("A1").format.rowHeight = 30;
  sheet.getRange("A2:H2").merge();
  sheet.getRange("A2").values = [[subtitle]];
  sheet.getRange("A2").format = {
    fill: theme.gray100,
    font: { color: theme.gray700, italic: true },
    wrapText: true,
  };
  sheet.getRange("A2").format.rowHeight = 42;
}

function writeTable(sheet, startCell, headers, rows, tableName) {
  const start = parseA1(startCell);
  const matrix = [headers, ...rows];
  const range = sheet.getRangeByIndexes(start.row, start.col, matrix.length, headers.length);
  range.values = matrix;
  const headerRange = sheet.getRangeByIndexes(start.row, start.col, 1, headers.length);
  headerRange.format = {
    fill: theme.teal,
    font: { bold: true, color: theme.white },
    wrapText: true,
    borders: { preset: "all", style: "thin", color: theme.border },
  };
  const bodyRange = sheet.getRangeByIndexes(start.row + 1, start.col, rows.length, headers.length);
  bodyRange.format = {
    fill: theme.white,
    font: { color: theme.gray900 },
    wrapText: true,
    borders: { preset: "all", style: "thin", color: theme.border },
  };
  if (rows.length > 0) {
    try {
      const endCell = toA1(start.row + rows.length, start.col + headers.length - 1);
      const table = sheet.tables.add(`${startCell}:${endCell}`, true, tableName);
      table.style = "TableStyleMedium2";
      table.showFilterButton = true;
    } catch {
      // Formatting above is enough if the table API rejects a name/range.
    }
  }
  return { start, rowCount: matrix.length, colCount: headers.length };
}

function addValidation(sheet, range, values) {
  sheet.getRange(range).dataValidation = { rule: { type: "list", values } };
}

function addConditionalStatus(sheet, range) {
  const r = sheet.getRange(range);
  r.conditionalFormats.add("containsText", { text: "ผ่าน", format: { fill: "#DCFCE7", font: { color: "#166534" } } });
  r.conditionalFormats.add("containsText", { text: "ต้องปรับ", format: { fill: "#FEF3C7", font: { color: "#92400E" } } });
  r.conditionalFormats.add("containsText", { text: "ยังขาด", format: { fill: "#FEE2E2", font: { color: "#991B1B" } } });
  r.conditionalFormats.add("containsText", { text: "ไม่เกี่ยว", format: { fill: "#E5E7EB", font: { color: "#374151" } } });
}

function parseA1(a1) {
  const [, letters, row] = /^([A-Z]+)(\d+)$/i.exec(a1);
  let col = 0;
  for (const ch of letters.toUpperCase()) col = col * 26 + (ch.charCodeAt(0) - 64);
  return { row: Number(row) - 1, col: col - 1 };
}

function toA1(rowZero, colZero) {
  let n = colZero + 1;
  let letters = "";
  while (n > 0) {
    const rem = (n - 1) % 26;
    letters = String.fromCharCode(65 + rem) + letters;
    n = Math.floor((n - 1) / 26);
  }
  return `${letters}${rowZero + 1}`;
}

function autofit(sheet, usedRange = "A:H") {
  sheet.getRange(usedRange).format.autofitColumns();
  sheet.getRange(usedRange).format.autofitRows();
}

const statusValues = ["ยังไม่ได้ทดสอบ", "ผ่าน", "ต้องปรับ", "ยังขาด", "ไม่เกี่ยว"];
const priorityValues = ["P0 Go-live", "P1 สำคัญ", "P2 ทำต่อ", "P3 Nice to have"];
const yesNoValues = ["ต้องการ", "ไม่ต้องการ", "ไม่แน่ใจ"];

const overview = workbook.worksheets.add("Overview");
title(
  overview,
  "PonPon Backend Requirement Questionnaire",
  "ไฟล์นี้ใช้ให้ลูกค้าทดสอบระบบหลังบ้าน/LIFF shop แล้วกรอก feedback requirement กลับมา โดยอิงจาก module ที่มีอยู่ใน PonPon backend ปัจจุบัน"
);
setWidth(overview, [22, 36, 22, 22, 28, 24, 24, 40]);
overview.getRange("A4:B9").values = [
  ["โปรเจกต์", "PonPon"],
  ["ผู้ให้ feedback", ""],
  ["บริษัท/ร้านค้า", ""],
  ["วันที่ทดสอบ", ""],
  ["เวอร์ชันเอกสาร", "v0.1"],
  ["หมายเหตุรวม", ""],
];
overview.getRange("A4:A9").format = { fill: theme.gray100, font: { bold: true }, borders: { preset: "all", style: "thin", color: theme.border } };
overview.getRange("B4:B9").format = { fill: theme.white, borders: { preset: "all", style: "thin", color: theme.border }, wrapText: true };
writeTable(
  overview,
  "A12",
  ["หมวดทดสอบ", "รายละเอียด", "สถานะ", "Priority", "Owner", "Feedback/คำถาม"],
  [
    ["Dashboard", "ยอดขาย, order/payment status, low stock, sync health", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Admin users & permissions", "Role/permission หลังบ้าน", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Catalog / ZORT sync", "สินค้า, stock, variant, visibility, warehouse sync", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Home slide", "Banner, CTA, publish/unpublish, reorder", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", ""],
    ["Flash sale", "Sale price, quota, active campaign", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", ""],
    ["Coupon / campaign / promotion", "Coupon scope, bulk generate, usage, audit log", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Order / checkout", "Pricing preview, create order, cancel, return, refund", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Payment / Omise", "PromptPay, mobile banking, card, webhook", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Shipping / SHIPPOP", "Rate, booking, tracking, label, webhook", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", ""],
    ["Customer / engagement", "LINE login, address, wishlist, recently viewed", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", ""],
    ["Review", "Review, media, moderation", "ยังไม่ได้ทดสอบ", "P2 ทำต่อ", "", ""],
    ["Notification", "LIFF notification, LINE, SignalR realtime", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", ""],
    ["Integration / settings", "ZORT, Omise, LINE, Supabase, SHIPPOP, webhooks", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", ""],
    ["Report / export", "Sales/order/payment/shipping/customer reports", "ยังไม่ได้ทดสอบ", "P2 ทำต่อ", "", ""],
  ],
  "OverviewScope"
);
addValidation(overview, "C13:C60", statusValues);
addValidation(overview, "D13:D60", priorityValues);
addConditionalStatus(overview, "C13:C60");
overview.freezePanes.freezeRows(12);

const uat = workbook.worksheets.add("UAT Scenarios");
title(uat, "UAT Scenarios", "ให้ลูกค้าลองทำ scenario แล้วกรอกผลทดสอบ, priority และ feedback ที่ต้องการให้ทีม dev/PM follow-up");
setWidth(uat, [28, 44, 18, 18, 24, 46, 24, 22]);
writeTable(
  uat,
  "A4",
  ["Scenario", "ผลที่คาดหวัง", "สถานะ", "Priority", "ผู้รับผิดชอบ", "Feedback/Requirement เพิ่มเติม", "หลักฐาน/ลิงก์", "วันที่"],
  [
    ["Login admin", "เข้า dashboard ได้ตามสิทธิ์", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["Sync product จาก ZORT", "สินค้า/stock/variant อัปเดตถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["ตั้งสินค้าให้แสดงบน LIFF", "ลูกค้าเห็นสินค้าใน shop", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["ตั้ง home slide", "Banner แสดงถูกลำดับและ link ถูกต้อง", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["ตั้ง flash sale", "ราคาพิเศษและ quota ถูกต้อง", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["สร้าง coupon fixed", "ส่วนลด fixed ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["สร้าง coupon percentage", "ส่วนลด percentage และ max discount ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["Bulk generate coupon", "Code ถูกสร้างและผูก campaign", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["ลูกค้า LINE login", "Profile ถูกสร้าง/อัปเดต", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["ลูกค้าเพิ่มที่อยู่", "Address/default address ถูกต้อง", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["ลูกค้าสร้าง order", "Order เข้า PonPon/ZORT ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["Pricing preview", "ราคา, ค่าส่ง, coupon, flash sale ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["ชำระ PromptPay", "Payment status update ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["ชำระ credit card", "Payment status update ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["สร้าง shipping booking", "ได้ tracking/label", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["Cancel order", "สถานะและ stock/quota/coupon ถูกต้อง", "ยังไม่ได้ทดสอบ", "P0 Go-live", "", "", "", ""],
    ["Return request", "Admin/customer เห็นสถานะถูกต้อง", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["Confirm received", "Order ปิดงานถูกต้อง", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
    ["Review สินค้า", "Review แสดง/รอ approve ตาม rule", "ยังไม่ได้ทดสอบ", "P2 ทำต่อ", "", "", "", ""],
    ["Notification", "ลูกค้า/admin ได้รับแจ้งเตือน", "ยังไม่ได้ทดสอบ", "P1 สำคัญ", "", "", "", ""],
  ],
  "UatScenarios"
);
addValidation(uat, "C5:C80", statusValues);
addValidation(uat, "D5:D80", priorityValues);
addConditionalStatus(uat, "C5:C80");
uat.freezePanes.freezeRows(4);

const permissions = workbook.worksheets.add("Permissions");
title(permissions, "Admin Roles & Permissions", "ยืนยัน role หลังบ้านและ permission ที่แต่ละทีมต้องใช้");
setWidth(permissions, [28, 44, 18, 26, 32, 44, 18, 18]);
writeTable(
  permissions,
  "A4",
  ["Permission", "ความหมาย", "ต้องการใช้ไหม", "Role ที่ควรได้สิทธิ์", "ผู้ใช้งานจริง", "หมายเหตุ", "Priority"],
  [
    ["dashboard.read", "ดู dashboard", "ต้องการ", "Owner, Admin, Manager", "", "", "P0 Go-live"],
    ["orders.read", "ดู order", "ต้องการ", "Owner, Admin, CS/Fulfillment", "", "", "P0 Go-live"],
    ["orders.manage", "จัดการ order / cancel / return", "ต้องการ", "Owner, Admin, CS/Fulfillment", "", "", "P0 Go-live"],
    ["orders.refund", "อนุมัติ manual refund", "ไม่แน่ใจ", "Owner, Manager", "", "", "P1 สำคัญ"],
    ["products.read", "ดูสินค้า", "ต้องการ", "Owner, Admin, Marketing", "", "", "P0 Go-live"],
    ["products.manage", "จัดการสินค้า/รูป/visibility/PonPon settings", "ต้องการ", "Owner, Admin, Product team", "", "", "P0 Go-live"],
    ["customers.read", "ดูข้อมูลลูกค้า", "ต้องการ", "Owner, Admin, CS", "", "", "P1 สำคัญ"],
    ["reviews.read", "ดู review", "ต้องการ", "Owner, Admin, CS/Marketing", "", "", "P2 ทำต่อ"],
    ["reviews.manage", "Approve/reject/delete review", "ไม่แน่ใจ", "Owner, Admin, CS", "", "", "P2 ทำต่อ"],
    ["marketing.manage", "Coupon, campaign, promotion, flash sale", "ต้องการ", "Owner, Admin, Marketing", "", "", "P0 Go-live"],
    ["integrations.read", "ดูค่า integration", "ต้องการ", "Owner, Admin, Tech", "", "", "P0 Go-live"],
    ["integrations.manage", "แก้ integration/register webhook", "ไม่แน่ใจ", "Owner, Tech", "", "", "P0 Go-live"],
    ["settings.manage", "ตั้งค่าระบบ", "ไม่แน่ใจ", "Owner, Tech", "", "", "P1 สำคัญ"],
    ["admin_users.read", "ดู user หลังบ้าน", "ต้องการ", "Owner, Admin", "", "", "P0 Go-live"],
    ["admin_users.manage", "สร้าง/แก้ไข/reset password user หลังบ้าน", "ไม่แน่ใจ", "Owner", "", "", "P0 Go-live"],
    ["*", "Owner ทำได้ทุกอย่าง", "ต้องการ", "Owner", "", "", "P0 Go-live"],
  ],
  "Permissions"
);
addValidation(permissions, "C5:C80", yesNoValues);
addValidation(permissions, "G5:G80", priorityValues);

const featureSheets = [
  {
    name: "Catalog",
    sub: "สินค้า, ZORT sync, warehouse, visibility, รูปสินค้า และ PonPon-specific settings",
    rows: [
      ["Source of truth", "ให้ ZORT เป็นแหล่งข้อมูลหลักของสินค้า/stock ใช่หรือไม่", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Product sync", "ต้องการ sync อัตโนมัติ/กด manual/retry เมื่อ fail อย่างไร", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Missing product", "สินค้าที่หายจาก ZORT ให้ซ่อนจาก LIFF อัตโนมัติไหม", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Variant", "รูปแบบ variant จริง เช่น สี/ขนาด/รสชาติ", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Stock reservation", "กัน stock ตอน checkout หรือ payment success", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Rich description", "ต้องการ editor แบบ plain text/HTML/image upload", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Product badge", "ต้องการ badge เช่น New, Best Seller, Flash Sale", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Warehouse", "ต้องแยก warehouse หรือใช้ stock รวม", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Product image", "ต้องใช้รูป mobile/desktop หรือ primary image เท่านั้น", "ไม่แน่ใจ", "P1 สำคัญ", ""],
    ],
  },
  {
    name: "Marketing",
    sub: "Home slide, Flash sale, Coupon, Campaign, Promotion และ discount business rules",
    rows: [
      ["Home slide", "ต้องการ title, description, badge, CTA, publish/unpublish, reorder หรือไม่", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Flash sale time", "Flash sale เริ่ม/จบตาม timezone ใด", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Flash sale quota", "จำกัด quota ต่อ campaign/สินค้า/customer อย่างไร", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Coupon stacking", "ใช้ coupon ได้สูงสุดกี่ใบต่อ order และลำดับคำนวณอย่างไร", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Coupon + flash sale", "Coupon ใช้ร่วมกับ flash sale ได้ไหม", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Coupon scope", "ต้องมี scope product/variant/SKU/category/customer หรือไม่", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Bulk generate", "ต้อง bulk generate และ export coupon code ไหม", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Campaign report", "ต้องดู generated/redeemed/remaining/discount amount ไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Audit log", "ต้อง audit log การแก้ coupon/campaign ระดับไหน", "ไม่แน่ใจ", "P1 สำคัญ", ""],
    ],
  },
  {
    name: "Orders",
    sub: "Checkout, pricing preview, order status, cancel, return, refund และ export",
    rows: [
      ["Order number", "ใช้เลขจาก ZORT หรือ PonPon generate เอง", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Pricing preview", "ต้องแสดง item subtotal, shipping, coupon, VAT, grand total อย่างไร", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Payment timeout", "ให้รอชำระเงินกี่นาที/ชั่วโมง", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Cancel by customer", "ลูกค้ายกเลิกได้ถึงสถานะไหน", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Cancel by admin", "Admin ยกเลิกได้ทุกสถานะไหม", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Confirm received", "ต้อง auto confirm หลังจัดส่งกี่วัน", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Return window", "ลูกค้าขอคืนสินค้าได้ภายในกี่วัน", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Refund rule", "คืนเต็มจำนวน/บางส่วน/ค่าส่ง อย่างไร", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Order export", "Export order ต้องมี field อะไรบ้าง", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
    ],
  },
  {
    name: "Payment Shipping",
    sub: "Omise payment, SHIPPOP shipping rate/booking/tracking/COD",
    rows: [
      ["Payment methods", "ใช้ PromptPay, mobile banking, credit card, COD, bank transfer หรือไม่", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Payment retry", "ถ้าจ่ายไม่สำเร็จให้ retry ได้ไหม", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Omise webhook", "เมื่อ payment success ต้อง update ZORT/order status อย่างไร", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Refund via Omise", "Refund ผ่าน Omise API หรือ manual", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Courier", "ใช้ขนส่งเจ้าไหนบ้าง", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Shipping selection", "ให้ลูกค้าเลือกเอง หรือระบบเลือกถูกสุด/เร็วสุด", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Sender address", "มีผู้ส่งกี่ที่อยู่", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Parcel size", "ใช้ขนาดจากสินค้า หรือให้ admin กรอกตอน booking", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["COD", "ต้องรองรับ COD ไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Tracking & label", "ต้องแสดง tracking/print label ให้ลูกค้า/admin ไหม", "ไม่แน่ใจ", "P1 สำคัญ", ""],
    ],
  },
  {
    name: "Customer Review Noti",
    sub: "ลูกค้า, address, wishlist, recently viewed, review และ notification",
    rows: [
      ["Customer login", "ลูกค้า login ผ่าน LINE เท่านั้นหรือมีช่องทางอื่น", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Customer profile", "ต้องเก็บ email/phone/profile เพิ่มอะไรบ้าง", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Address", "ลูกค้ามีหลายที่อยู่และ default address หรือไม่", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Wishlist", "ต้องการ wishlist ใน phase แรกไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Recently viewed", "ต้องการ recently viewed ใน phase แรกไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Review eligibility", "ต้องซื้อก่อนถึง review ได้ใช่ไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Review moderation", "Review ต้อง approve ก่อนแสดงไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Review media", "รองรับรูป/วิดีโอ กี่ไฟล์ ขนาดเท่าไหร่", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Notification channel", "แจ้งผ่าน LIFF, LINE, email, realtime admin เรื่องใดบ้าง", "ไม่แน่ใจ", "P1 สำคัญ", ""],
    ],
  },
  {
    name: "Integrations",
    sub: "ZORT, Omise, LINE, Supabase Storage, SHIPPOP, Hangfire และ webhook/sync operation",
    rows: [
      ["ZORT credential", "ใครเป็นเจ้าของ credential และให้แก้ในหลังบ้านไหม", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["ZORT webhooks", "ต้อง register/check webhook ผ่านหลังบ้านไหม", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Omise keys", "ต้อง mask secret และให้ใครแก้ได้", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["LINE LIFF/Login", "ต้อง config channel และ LIFF URL อย่างไร", "ไม่แน่ใจ", "P0 Go-live", ""],
      ["Supabase Storage", "ใช้เก็บรูป product/review/rich text และ policy อย่างไร", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["SHIPPOP credential", "ใครแก้ sender/API settings ได้", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Sync history", "ต้องมีหน้าแสดงประวัติ sync/retry/fail reason ไหม", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Webhook raw payload", "ต้องเก็บ raw payload และ audit trail ไหม", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["Alert on fail", "Sync/webhook fail ต้องแจ้งเตือนใคร ช่องทางไหน", "ไม่แน่ใจ", "P1 สำคัญ", ""],
    ],
  },
  {
    name: "Reports Security",
    sub: "รายงาน, export, audit, PDPA, 2FA และ security requirement",
    rows: [
      ["Sales report", "ต้องมี sales/order/payment/refund/return/shipping report อะไรบ้าง", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Product report", "ต้องมี product sales/low stock/coupon usage/campaign performance ไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Customer report", "ต้องมี customer LTV/segment/export ไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["Scheduled report", "ต้องส่ง report อัตโนมัติทาง email ไหม", "ไม่แน่ใจ", "P3 Nice to have", ""],
      ["Admin audit", "ต้อง audit log order/settings/integration/user action แค่ไหน", "ไม่แน่ใจ", "P1 สำคัญ", ""],
      ["2FA", "ต้องมี 2FA สำหรับ admin ไหม", "ไม่แน่ใจ", "P2 ทำต่อ", ""],
      ["IP allowlist", "ต้องจำกัด IP หลังบ้านไหม", "ไม่แน่ใจ", "P3 Nice to have", ""],
      ["PDPA consent", "ต้องเก็บ consent/ลบบัญชี/data retention อย่างไร", "ไม่แน่ใจ", "P1 สำคัญ", ""],
    ],
  },
];

for (const fsheet of featureSheets) {
  const sheet = workbook.worksheets.add(fsheet.name);
  title(sheet, fsheet.name, fsheet.sub);
  setWidth(sheet, [28, 58, 18, 18, 54, 26, 20, 20]);
  writeTable(sheet, "A4", ["หัวข้อ", "คำถาม/Requirement ที่ต้องยืนยัน", "คำตอบ", "Priority", "Feedback/รายละเอียดจากลูกค้า"], fsheet.rows, `${fsheet.name.replaceAll(" ", "")}Req`);
  addValidation(sheet, "C5:C80", yesNoValues);
  addValidation(sheet, "D5:D80", priorityValues);
  sheet.freezePanes.freezeRows(4);
}

const api = workbook.worksheets.add("API Reference");
title(api, "Current Backend Scope Reference", "สรุป endpoint/module สำคัญจาก code ปัจจุบัน ใช้เป็น reference ระหว่าง UAT และ requirement discussion");
setWidth(api, [24, 48, 64, 28, 24, 34]);
writeTable(
  api,
  "A4",
  ["Module", "Endpoints/Scope", "สิ่งที่ระบบทำได้", "Requirement ที่ควรยืนยัน", "Priority", "หมายเหตุ"],
  [
    ["Identity", "api/admin/auth, api/admin/users, api/auth, api/customer-addresses", "Admin login, first admin, LINE login, refresh token, customer addresses, admin users", "Role/permission และ profile fields", "P0 Go-live", ""],
    ["Catalog", "api/admin/products, api/products, api/shop/products, api/categories, ZORT product webhooks", "Product list/detail, visibility, images, ZORT sync, categories, warehouses", "ZORT source of truth, stock rule, product enrichment", "P0 Go-live", ""],
    ["Home", "api/shop/home, api/home-slides, api/admin/home-slides", "Home content และ slide management", "Banner fields, schedule, link behavior", "P1 สำคัญ", ""],
    ["Flash sale", "api/flash-sales/active, api/admin/flash-sales", "Active flash sale และ admin CRUD", "Quota, timing, coupon compatibility", "P1 สำคัญ", ""],
    ["Promotion", "api/admin/coupons, api/admin/coupon-campaigns, api/admin/promotions, api/shop/coupons", "Coupon, campaign, bulk generate, usage, audit, customer coupon", "Discount rules, stacking, scope, claim behavior", "P0 Go-live", ""],
    ["Ordering", "api/orders, api/admin/orders, api/webhooks/zort/order", "Pricing preview, create order, my orders, admin order, cancel, return, refund, export, ZORT sync", "Order status lifecycle, refund/return policy", "P0 Go-live", ""],
    ["Payment", "api/payments, api/webhooks/omise", "Omise config, PromptPay, mobile banking, card, charge status", "Payment methods, timeout, retry, refund", "P0 Go-live", ""],
    ["Shipping", "api/shipping, api/admin/shipping, api/webhooks/shippop", "Rate, booking, tracking, cancel booking, sender config", "Courier, parcel rules, COD, tracking notification", "P1 สำคัญ", ""],
    ["Reviews", "api/products/{id}/reviews, api/reviews, api/admin/reviews", "Public reviews, review media, moderation, admin review actions", "Eligibility, moderation, media limits", "P2 ทำต่อ", ""],
    ["Notification", "api/notifications, /hubs/shop-notifications, LINE notifier", "Shop notification list, unread count, mark read, realtime, LINE order/cancel", "Notification events/channels", "P1 สำคัญ", ""],
    ["Settings", "api/admin/settings/integrations", "Integration settings, ZORT webhook check/register", "Credential ownership, masking, edit permission", "P0 Go-live", ""],
    ["Dashboard", "api/admin/dashboard, api/admin/dashboard/sync-runs", "Sales/order/payment/inventory/shipping/sync summary", "KPI, filter, timezone, report needs", "P0 Go-live", ""],
  ],
  "ApiReference"
);
addValidation(api, "E5:E50", priorityValues);
api.freezePanes.freezeRows(4);

const summary = workbook.worksheets.add("Dev Summary");
title(summary, "Dev/PM Summary", "ส่วนนี้ทีม dev/PM กรอกหลังได้รับ feedback เพื่อสรุป scope, risk และ estimate");
setWidth(summary, [34, 90, 24, 24, 24, 24, 24, 24]);
writeTable(
  summary,
  "A4",
  ["หัวข้อ", "สรุป", "Owner", "Priority", "สถานะ"],
  [
    ["Requirement ที่ confirm แล้ว", "", "", "P0 Go-live", "ยังไม่ได้ทดสอบ"],
    ["Requirement ที่ต้องถามเพิ่ม", "", "", "P0 Go-live", "ยังไม่ได้ทดสอบ"],
    ["Scope เพิ่มจากระบบปัจจุบัน", "", "", "P1 สำคัญ", "ยังไม่ได้ทดสอบ"],
    ["Scope ที่ควรตัด/เลื่อนไป phase ถัดไป", "", "", "P2 ทำต่อ", "ยังไม่ได้ทดสอบ"],
    ["Risk ด้าน integration", "", "", "P0 Go-live", "ยังไม่ได้ทดสอบ"],
    ["Risk ด้านข้อมูล/operation", "", "", "P1 สำคัญ", "ยังไม่ได้ทดสอบ"],
    ["Impact ต่อ backend", "", "", "P0 Go-live", "ยังไม่ได้ทดสอบ"],
    ["Impact ต่อ frontend/admin", "", "", "P0 Go-live", "ยังไม่ได้ทดสอบ"],
    ["Impact ต่อ database/migration", "", "", "P1 สำคัญ", "ยังไม่ได้ทดสอบ"],
    ["Estimate เบื้องต้น", "", "", "P0 Go-live", "ยังไม่ได้ทดสอบ"],
  ],
  "DevSummary"
);
addValidation(summary, "D5:D30", priorityValues);
addValidation(summary, "E5:E30", statusValues);
addConditionalStatus(summary, "E5:E30");
summary.freezePanes.freezeRows(4);

for (const sheet of workbook.worksheets.items) {
  const used = sheet.getUsedRange();
  if (used) {
    try {
      used.format.autofitColumns();
      used.format.autofitRows();
    } catch {}
  }
}

for (const name of workbook.worksheets.items.map((s) => s.name)) {
  const preview = await workbook.render({ sheetName: name, autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(`${outputDir}/preview-${name.replaceAll(" ", "-")}.png`, new Uint8Array(await preview.arrayBuffer()));
}

const errorScan = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 50 },
  summary: "formula error scan",
});
console.log(errorScan.ndjson);

const overviewInspect = await workbook.inspect({
  kind: "table",
  sheetId: "Overview",
  range: "A12:F27",
  include: "values",
  tableMaxRows: 20,
  tableMaxCols: 8,
  maxChars: 4000,
});
console.log(overviewInspect.ndjson);

await fs.mkdir(outputDir, { recursive: true });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
console.log(outputPath);
