import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import dayjs from 'dayjs';
import { ordersApi, ordersTransferApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const drawerOpen = ref(false);
const detail = ref(null);
const dateRange = ref(null);
const technicians = ref([]);
const transferOpen = ref(false);
const transferTarget = ref(null);
const transferTo = ref(null);
const transferReason = ref('');
const transferring = ref(false);
function openTransfer(row) {
    transferTarget.value = row;
    transferTo.value = null;
    transferReason.value = '';
    transferOpen.value = true;
}
async function doTransfer() {
    if (!detail.value || !transferTarget.value || !transferTo.value) {
        ElMessage.warning('请选择新技师');
        return;
    }
    transferring.value = true;
    try {
        detail.value = await ordersTransferApi.transfer(detail.value.id, transferTarget.value.id, {
            newTechnicianId: transferTo.value,
            reason: transferReason.value || null
        });
        ElMessage.success('转钟成功');
        transferOpen.value = false;
    }
    catch {
        /* http 已弹错 */
    }
    finally {
        transferring.value = false;
    }
}
const query = reactive({
    page: 1,
    pageSize: 20,
    status: ''
});
function payLabel(m) {
    return {
        Cash: '现金', MemberCard: '会员卡', Wechat: '微信', Alipay: '支付宝', BankCard: '银行卡', Unpaid: '未支付'
    }[m] ?? m;
}
function statusLabel(s) {
    return {
        Pending: '待结账', InProgress: '服务中', Completed: '已完成', Cancelled: '已取消', Refunded: '已退款'
    }[s] ?? s;
}
function statusType(s) {
    return {
        Pending: 'warning', InProgress: 'info', Completed: 'success', Cancelled: 'info', Refunded: 'danger'
    }[s] ?? '';
}
function formatTime(v) {
    return dayjs(v).format('YYYY-MM-DD HH:mm');
}
async function reload() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        const data = await ordersApi.list({
            page: query.page,
            pageSize: query.pageSize,
            storeId: appStore.activeStoreId,
            status: query.status || undefined,
            from: dateRange.value?.[0],
            to: dateRange.value?.[1]
        });
        rows.value = data.items;
        total.value = data.total;
    }
    finally {
        loading.value = false;
    }
}
function resetQuery() {
    query.status = '';
    dateRange.value = null;
    query.page = 1;
    reload();
}
async function openDetail(row) {
    drawerOpen.value = true;
    detail.value = await ordersApi.get(row.id);
}
async function onRefund() {
    if (!detail.value)
        return;
    const reason = await ElMessageBox.prompt('请输入退款原因', '确认退款', {
        inputPlaceholder: '可选',
        confirmButtonText: '确认退款',
        cancelButtonText: '取消',
        type: 'warning'
    }).catch(() => null);
    if (!reason)
        return;
    await ordersApi.refund(detail.value.id, reason.value || null);
    ElMessage.success('已退款');
    drawerOpen.value = false;
    reload();
}
async function onCancel() {
    if (!detail.value)
        return;
    await ElMessageBox.confirm('确认取消该订单？', '提示', { type: 'warning' }).catch(() => null);
    await ordersApi.cancel(detail.value.id);
    ElMessage.success('已取消');
    drawerOpen.value = false;
    reload();
}
onMounted(async () => {
    await appStore.loadStores();
    await reload();
    staffApi.list({ role: 'Technician', pageSize: 200, storeId: appStore.activeStoreId ?? undefined })
        .then((r) => { technicians.value = r.items; })
        .catch(() => null);
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "page" },
});
const __VLS_0 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    shadow: "never",
}));
const __VLS_2 = __VLS_1({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_4 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    modelValue: (__VLS_ctx.query.status),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    modelValue: (__VLS_ctx.query.status),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
__VLS_7.slots.default;
const __VLS_8 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    label: "待结账",
    value: "Pending",
}));
const __VLS_10 = __VLS_9({
    label: "待结账",
    value: "Pending",
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
const __VLS_12 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    label: "已完成",
    value: "Completed",
}));
const __VLS_14 = __VLS_13({
    label: "已完成",
    value: "Completed",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
const __VLS_16 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    label: "已取消",
    value: "Cancelled",
}));
const __VLS_18 = __VLS_17({
    label: "已取消",
    value: "Cancelled",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
const __VLS_20 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    label: "已退款",
    value: "Refunded",
}));
const __VLS_22 = __VLS_21({
    label: "已退款",
    value: "Refunded",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
var __VLS_7;
const __VLS_24 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    modelValue: (__VLS_ctx.dateRange),
    type: "datetimerange",
    rangeSeparator: "至",
    startPlaceholder: "开始时间",
    endPlaceholder: "结束时间",
    format: "YYYY-MM-DD HH:mm",
    valueFormat: "YYYY-MM-DDTHH:mm:ss[Z]",
}));
const __VLS_26 = __VLS_25({
    modelValue: (__VLS_ctx.dateRange),
    type: "datetimerange",
    rangeSeparator: "至",
    startPlaceholder: "开始时间",
    endPlaceholder: "结束时间",
    format: "YYYY-MM-DD HH:mm",
    valueFormat: "YYYY-MM-DDTHH:mm:ss[Z]",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
const __VLS_28 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_30 = __VLS_29({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_31.slots.default;
var __VLS_31;
const __VLS_36 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    ...{ 'onClick': {} },
}));
const __VLS_38 = __VLS_37({
    ...{ 'onClick': {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
let __VLS_40;
let __VLS_41;
let __VLS_42;
const __VLS_43 = {
    onClick: (__VLS_ctx.resetQuery)
};
__VLS_39.slots.default;
var __VLS_39;
const __VLS_44 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    ...{ 'onRowClick': {} },
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_46 = __VLS_45({
    ...{ 'onRowClick': {} },
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
let __VLS_48;
let __VLS_49;
let __VLS_50;
const __VLS_51 = {
    onRowClick: (__VLS_ctx.openDetail)
};
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_47.slots.default;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "orderNo",
    label: "订单号",
    minWidth: "180",
}));
const __VLS_54 = __VLS_53({
    prop: "orderNo",
    label: "订单号",
    minWidth: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    label: "项数",
    width: "60",
    prop: "itemCount",
}));
const __VLS_58 = __VLS_57({
    label: "项数",
    width: "60",
    prop: "itemCount",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "金额",
    width: "100",
}));
const __VLS_62 = __VLS_61({
    label: "金额",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.total.toFixed(2));
}
var __VLS_63;
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    label: "实收",
    width: "100",
}));
const __VLS_66 = __VLS_65({
    label: "实收",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_67.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_67.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.paidAmount.toFixed(2));
}
var __VLS_67;
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    label: "支付",
    width: "100",
}));
const __VLS_70 = __VLS_69({
    label: "支付",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
__VLS_71.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_71.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.payLabel(row.payMethod));
}
var __VLS_71;
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    label: "状态",
    width: "100",
}));
const __VLS_74 = __VLS_73({
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_75.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_76 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
        type: (__VLS_ctx.statusType(row.status)),
    }));
    const __VLS_78 = __VLS_77({
        type: (__VLS_ctx.statusType(row.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_77));
    __VLS_79.slots.default;
    (__VLS_ctx.statusLabel(row.status));
    var __VLS_79;
}
var __VLS_75;
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    label: "会员",
    width: "120",
    prop: "memberCardNo",
}));
const __VLS_82 = __VLS_81({
    label: "会员",
    width: "120",
    prop: "memberCardNo",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
__VLS_83.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_83.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.memberCardNo ?? '—');
}
var __VLS_83;
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    label: "时间",
    minWidth: "160",
}));
const __VLS_86 = __VLS_85({
    label: "时间",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.formatTime(row.createdAt));
}
var __VLS_87;
var __VLS_47;
const __VLS_88 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}));
const __VLS_90 = __VLS_89({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
let __VLS_92;
let __VLS_93;
let __VLS_94;
const __VLS_95 = {
    onCurrentChange: ((p) => { __VLS_ctx.query.page = p; __VLS_ctx.reload(); })
};
const __VLS_96 = {
    onSizeChange: ((s) => { __VLS_ctx.query.pageSize = s; __VLS_ctx.query.page = 1; __VLS_ctx.reload(); })
};
var __VLS_91;
var __VLS_3;
const __VLS_97 = {}.ElDrawer;
/** @type {[typeof __VLS_components.ElDrawer, typeof __VLS_components.elDrawer, typeof __VLS_components.ElDrawer, typeof __VLS_components.elDrawer, ]} */ ;
// @ts-ignore
const __VLS_98 = __VLS_asFunctionalComponent(__VLS_97, new __VLS_97({
    modelValue: (__VLS_ctx.drawerOpen),
    title: "订单详情",
    size: "520px",
}));
const __VLS_99 = __VLS_98({
    modelValue: (__VLS_ctx.drawerOpen),
    title: "订单详情",
    size: "520px",
}, ...__VLS_functionalComponentArgsRest(__VLS_98));
__VLS_100.slots.default;
if (__VLS_ctx.detail) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    const __VLS_101 = {}.ElDescriptions;
    /** @type {[typeof __VLS_components.ElDescriptions, typeof __VLS_components.elDescriptions, typeof __VLS_components.ElDescriptions, typeof __VLS_components.elDescriptions, ]} */ ;
    // @ts-ignore
    const __VLS_102 = __VLS_asFunctionalComponent(__VLS_101, new __VLS_101({
        column: (2),
        border: true,
    }));
    const __VLS_103 = __VLS_102({
        column: (2),
        border: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_102));
    __VLS_104.slots.default;
    const __VLS_105 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_106 = __VLS_asFunctionalComponent(__VLS_105, new __VLS_105({
        label: "订单号",
        span: (2),
    }));
    const __VLS_107 = __VLS_106({
        label: "订单号",
        span: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_106));
    __VLS_108.slots.default;
    (__VLS_ctx.detail.orderNo);
    var __VLS_108;
    const __VLS_109 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_110 = __VLS_asFunctionalComponent(__VLS_109, new __VLS_109({
        label: "状态",
    }));
    const __VLS_111 = __VLS_110({
        label: "状态",
    }, ...__VLS_functionalComponentArgsRest(__VLS_110));
    __VLS_112.slots.default;
    const __VLS_113 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_114 = __VLS_asFunctionalComponent(__VLS_113, new __VLS_113({
        type: (__VLS_ctx.statusType(__VLS_ctx.detail.status)),
    }));
    const __VLS_115 = __VLS_114({
        type: (__VLS_ctx.statusType(__VLS_ctx.detail.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_114));
    __VLS_116.slots.default;
    (__VLS_ctx.statusLabel(__VLS_ctx.detail.status));
    var __VLS_116;
    var __VLS_112;
    const __VLS_117 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_118 = __VLS_asFunctionalComponent(__VLS_117, new __VLS_117({
        label: "支付方式",
    }));
    const __VLS_119 = __VLS_118({
        label: "支付方式",
    }, ...__VLS_functionalComponentArgsRest(__VLS_118));
    __VLS_120.slots.default;
    (__VLS_ctx.payLabel(__VLS_ctx.detail.payMethod));
    var __VLS_120;
    const __VLS_121 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_122 = __VLS_asFunctionalComponent(__VLS_121, new __VLS_121({
        label: "合计",
    }));
    const __VLS_123 = __VLS_122({
        label: "合计",
    }, ...__VLS_functionalComponentArgsRest(__VLS_122));
    __VLS_124.slots.default;
    (__VLS_ctx.detail.total.toFixed(2));
    var __VLS_124;
    const __VLS_125 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_126 = __VLS_asFunctionalComponent(__VLS_125, new __VLS_125({
        label: "优惠",
    }));
    const __VLS_127 = __VLS_126({
        label: "优惠",
    }, ...__VLS_functionalComponentArgsRest(__VLS_126));
    __VLS_128.slots.default;
    (__VLS_ctx.detail.discountAmount.toFixed(2));
    var __VLS_128;
    const __VLS_129 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_130 = __VLS_asFunctionalComponent(__VLS_129, new __VLS_129({
        label: "实收",
    }));
    const __VLS_131 = __VLS_130({
        label: "实收",
    }, ...__VLS_functionalComponentArgsRest(__VLS_130));
    __VLS_132.slots.default;
    (__VLS_ctx.detail.paidAmount.toFixed(2));
    var __VLS_132;
    const __VLS_133 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_134 = __VLS_asFunctionalComponent(__VLS_133, new __VLS_133({
        label: "收银员",
    }));
    const __VLS_135 = __VLS_134({
        label: "收银员",
    }, ...__VLS_functionalComponentArgsRest(__VLS_134));
    __VLS_136.slots.default;
    (__VLS_ctx.detail.cashierName ?? '—');
    var __VLS_136;
    const __VLS_137 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_138 = __VLS_asFunctionalComponent(__VLS_137, new __VLS_137({
        label: "会员卡",
    }));
    const __VLS_139 = __VLS_138({
        label: "会员卡",
    }, ...__VLS_functionalComponentArgsRest(__VLS_138));
    __VLS_140.slots.default;
    (__VLS_ctx.detail.memberCardNo ?? '—');
    var __VLS_140;
    const __VLS_141 = {}.ElDescriptionsItem;
    /** @type {[typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, typeof __VLS_components.ElDescriptionsItem, typeof __VLS_components.elDescriptionsItem, ]} */ ;
    // @ts-ignore
    const __VLS_142 = __VLS_asFunctionalComponent(__VLS_141, new __VLS_141({
        label: "备注",
        span: (2),
    }));
    const __VLS_143 = __VLS_142({
        label: "备注",
        span: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_142));
    __VLS_144.slots.default;
    (__VLS_ctx.detail.remark ?? '—');
    var __VLS_144;
    var __VLS_104;
    const __VLS_145 = {}.ElDivider;
    /** @type {[typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, ]} */ ;
    // @ts-ignore
    const __VLS_146 = __VLS_asFunctionalComponent(__VLS_145, new __VLS_145({}));
    const __VLS_147 = __VLS_146({}, ...__VLS_functionalComponentArgsRest(__VLS_146));
    __VLS_148.slots.default;
    var __VLS_148;
    const __VLS_149 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_150 = __VLS_asFunctionalComponent(__VLS_149, new __VLS_149({
        data: (__VLS_ctx.detail.items),
        size: "small",
    }));
    const __VLS_151 = __VLS_150({
        data: (__VLS_ctx.detail.items),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_150));
    __VLS_152.slots.default;
    const __VLS_153 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_154 = __VLS_asFunctionalComponent(__VLS_153, new __VLS_153({
        prop: "serviceName",
        label: "项目",
    }));
    const __VLS_155 = __VLS_154({
        prop: "serviceName",
        label: "项目",
    }, ...__VLS_functionalComponentArgsRest(__VLS_154));
    const __VLS_157 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_158 = __VLS_asFunctionalComponent(__VLS_157, new __VLS_157({
        label: "技师",
        width: "120",
    }));
    const __VLS_159 = __VLS_158({
        label: "技师",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_158));
    __VLS_160.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_160.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (row.technicianName);
        if (row.transferredAt) {
            const __VLS_161 = {}.ElTag;
            /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
            // @ts-ignore
            const __VLS_162 = __VLS_asFunctionalComponent(__VLS_161, new __VLS_161({
                size: "small",
                type: "warning",
                ...{ style: {} },
            }));
            const __VLS_163 = __VLS_162({
                size: "small",
                type: "warning",
                ...{ style: {} },
            }, ...__VLS_functionalComponentArgsRest(__VLS_162));
            __VLS_164.slots.default;
            var __VLS_164;
        }
    }
    var __VLS_160;
    const __VLS_165 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_166 = __VLS_asFunctionalComponent(__VLS_165, new __VLS_165({
        label: "房间",
        width: "80",
    }));
    const __VLS_167 = __VLS_166({
        label: "房间",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_166));
    __VLS_168.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_168.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.roomNo ?? '—');
    }
    var __VLS_168;
    const __VLS_169 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_170 = __VLS_asFunctionalComponent(__VLS_169, new __VLS_169({
        prop: "quantity",
        label: "数量",
        width: "60",
    }));
    const __VLS_171 = __VLS_170({
        prop: "quantity",
        label: "数量",
        width: "60",
    }, ...__VLS_functionalComponentArgsRest(__VLS_170));
    const __VLS_173 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_174 = __VLS_asFunctionalComponent(__VLS_173, new __VLS_173({
        label: "金额",
        width: "100",
    }));
    const __VLS_175 = __VLS_174({
        label: "金额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_174));
    __VLS_176.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_176.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.itemTotal.toFixed(2));
    }
    var __VLS_176;
    const __VLS_177 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_178 = __VLS_asFunctionalComponent(__VLS_177, new __VLS_177({
        label: "提成",
        width: "100",
    }));
    const __VLS_179 = __VLS_178({
        label: "提成",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_178));
    __VLS_180.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_180.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.commissionAmount.toFixed(2));
    }
    var __VLS_180;
    if (__VLS_ctx.detail.status === 'Pending' || __VLS_ctx.detail.status === 'InProgress') {
        const __VLS_181 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_182 = __VLS_asFunctionalComponent(__VLS_181, new __VLS_181({
            label: "操作",
            width: "100",
        }));
        const __VLS_183 = __VLS_182({
            label: "操作",
            width: "100",
        }, ...__VLS_functionalComponentArgsRest(__VLS_182));
        __VLS_184.slots.default;
        {
            const { default: __VLS_thisSlot } = __VLS_184.slots;
            const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
            const __VLS_185 = {}.ElButton;
            /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
            // @ts-ignore
            const __VLS_186 = __VLS_asFunctionalComponent(__VLS_185, new __VLS_185({
                ...{ 'onClick': {} },
                size: "small",
            }));
            const __VLS_187 = __VLS_186({
                ...{ 'onClick': {} },
                size: "small",
            }, ...__VLS_functionalComponentArgsRest(__VLS_186));
            let __VLS_189;
            let __VLS_190;
            let __VLS_191;
            const __VLS_192 = {
                onClick: (...[$event]) => {
                    if (!(__VLS_ctx.detail))
                        return;
                    if (!(__VLS_ctx.detail.status === 'Pending' || __VLS_ctx.detail.status === 'InProgress'))
                        return;
                    __VLS_ctx.openTransfer(row);
                }
            };
            __VLS_188.slots.default;
            var __VLS_188;
        }
        var __VLS_184;
    }
    var __VLS_152;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "actions" },
    });
    if (__VLS_ctx.detail.status === 'Completed') {
        const __VLS_193 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_194 = __VLS_asFunctionalComponent(__VLS_193, new __VLS_193({
            ...{ 'onClick': {} },
            type: "danger",
        }));
        const __VLS_195 = __VLS_194({
            ...{ 'onClick': {} },
            type: "danger",
        }, ...__VLS_functionalComponentArgsRest(__VLS_194));
        let __VLS_197;
        let __VLS_198;
        let __VLS_199;
        const __VLS_200 = {
            onClick: (__VLS_ctx.onRefund)
        };
        __VLS_196.slots.default;
        var __VLS_196;
    }
    if (__VLS_ctx.detail.status === 'Pending') {
        const __VLS_201 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_202 = __VLS_asFunctionalComponent(__VLS_201, new __VLS_201({
            ...{ 'onClick': {} },
        }));
        const __VLS_203 = __VLS_202({
            ...{ 'onClick': {} },
        }, ...__VLS_functionalComponentArgsRest(__VLS_202));
        let __VLS_205;
        let __VLS_206;
        let __VLS_207;
        const __VLS_208 = {
            onClick: (__VLS_ctx.onCancel)
        };
        __VLS_204.slots.default;
        var __VLS_204;
    }
}
var __VLS_100;
const __VLS_209 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_210 = __VLS_asFunctionalComponent(__VLS_209, new __VLS_209({
    modelValue: (__VLS_ctx.transferOpen),
    title: "转钟（更换技师）",
    width: "420px",
}));
const __VLS_211 = __VLS_210({
    modelValue: (__VLS_ctx.transferOpen),
    title: "转钟（更换技师）",
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_210));
__VLS_212.slots.default;
const __VLS_213 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_214 = __VLS_asFunctionalComponent(__VLS_213, new __VLS_213({
    labelWidth: "90px",
}));
const __VLS_215 = __VLS_214({
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_214));
__VLS_216.slots.default;
const __VLS_217 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_218 = __VLS_asFunctionalComponent(__VLS_217, new __VLS_217({
    label: "项目",
}));
const __VLS_219 = __VLS_218({
    label: "项目",
}, ...__VLS_functionalComponentArgsRest(__VLS_218));
__VLS_220.slots.default;
(__VLS_ctx.transferTarget?.serviceName);
var __VLS_220;
const __VLS_221 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_222 = __VLS_asFunctionalComponent(__VLS_221, new __VLS_221({
    label: "原技师",
}));
const __VLS_223 = __VLS_222({
    label: "原技师",
}, ...__VLS_functionalComponentArgsRest(__VLS_222));
__VLS_224.slots.default;
(__VLS_ctx.transferTarget?.technicianName);
var __VLS_224;
const __VLS_225 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_226 = __VLS_asFunctionalComponent(__VLS_225, new __VLS_225({
    label: "新技师",
    required: true,
}));
const __VLS_227 = __VLS_226({
    label: "新技师",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_226));
__VLS_228.slots.default;
const __VLS_229 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_230 = __VLS_asFunctionalComponent(__VLS_229, new __VLS_229({
    modelValue: (__VLS_ctx.transferTo),
    placeholder: "选择新技师",
    filterable: true,
    ...{ style: {} },
}));
const __VLS_231 = __VLS_230({
    modelValue: (__VLS_ctx.transferTo),
    placeholder: "选择新技师",
    filterable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_230));
__VLS_232.slots.default;
for (const [t] of __VLS_getVForSourceType((__VLS_ctx.technicians))) {
    const __VLS_233 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_234 = __VLS_asFunctionalComponent(__VLS_233, new __VLS_233({
        key: (t.id),
        label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
        value: (t.id),
        disabled: (t.id === __VLS_ctx.transferTarget?.technicianId),
    }));
    const __VLS_235 = __VLS_234({
        key: (t.id),
        label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
        value: (t.id),
        disabled: (t.id === __VLS_ctx.transferTarget?.technicianId),
    }, ...__VLS_functionalComponentArgsRest(__VLS_234));
}
var __VLS_232;
var __VLS_228;
const __VLS_237 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_238 = __VLS_asFunctionalComponent(__VLS_237, new __VLS_237({
    label: "原因",
}));
const __VLS_239 = __VLS_238({
    label: "原因",
}, ...__VLS_functionalComponentArgsRest(__VLS_238));
__VLS_240.slots.default;
const __VLS_241 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_242 = __VLS_asFunctionalComponent(__VLS_241, new __VLS_241({
    modelValue: (__VLS_ctx.transferReason),
    maxlength: "200",
    placeholder: "如：客人要求换人 / 原技师有事",
}));
const __VLS_243 = __VLS_242({
    modelValue: (__VLS_ctx.transferReason),
    maxlength: "200",
    placeholder: "如：客人要求换人 / 原技师有事",
}, ...__VLS_functionalComponentArgsRest(__VLS_242));
var __VLS_240;
var __VLS_216;
{
    const { footer: __VLS_thisSlot } = __VLS_212.slots;
    const __VLS_245 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_246 = __VLS_asFunctionalComponent(__VLS_245, new __VLS_245({
        ...{ 'onClick': {} },
    }));
    const __VLS_247 = __VLS_246({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_246));
    let __VLS_249;
    let __VLS_250;
    let __VLS_251;
    const __VLS_252 = {
        onClick: (...[$event]) => {
            __VLS_ctx.transferOpen = false;
        }
    };
    __VLS_248.slots.default;
    var __VLS_248;
    const __VLS_253 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_254 = __VLS_asFunctionalComponent(__VLS_253, new __VLS_253({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.transferring),
    }));
    const __VLS_255 = __VLS_254({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.transferring),
    }, ...__VLS_functionalComponentArgsRest(__VLS_254));
    let __VLS_257;
    let __VLS_258;
    let __VLS_259;
    const __VLS_260 = {
        onClick: (__VLS_ctx.doTransfer)
    };
    __VLS_256.slots.default;
    var __VLS_256;
}
var __VLS_212;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['actions']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            rows: rows,
            total: total,
            loading: loading,
            drawerOpen: drawerOpen,
            detail: detail,
            dateRange: dateRange,
            technicians: technicians,
            transferOpen: transferOpen,
            transferTarget: transferTarget,
            transferTo: transferTo,
            transferReason: transferReason,
            transferring: transferring,
            openTransfer: openTransfer,
            doTransfer: doTransfer,
            query: query,
            payLabel: payLabel,
            statusLabel: statusLabel,
            statusType: statusType,
            formatTime: formatTime,
            reload: reload,
            resetQuery: resetQuery,
            openDetail: openDetail,
            onRefund: onRefund,
            onCancel: onCancel,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
