import { onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { complaintsApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);
const creating = ref(false);
const filter = reactive({ status: 'Pending', range: null, page: 1, pageSize: 20 });
const createForm = reactive({ orderItemId: 0, tags: '', comment: '' });
const resolveOpen = ref(false);
const resolving = ref(null);
const resolveForm = reactive({ resolution: 'Reassigned', reassignedToTechnicianId: null, resolutionNote: '' });
const techList = ref([]);
function statusLabel(s) {
    return { Pending: '待处理', Resolved: '已处理', Cancelled: '已取消' }[s] ?? s;
}
function statusTag(s) {
    return s === 'Pending' ? 'warning' : s === 'Resolved' ? 'success' : 'info';
}
function resolutionLabel(r) {
    return { Reassigned: '改派', Refunded: '退款', Apologized: '道歉/补偿', NoAction: '不予处理' }[r] ?? r;
}
function formatTime(t) {
    return t ? dayjs(t).format('YYYY-MM-DD HH:mm') : '';
}
async function reload() {
    loading.value = true;
    try {
        const res = await complaintsApi.list({
            storeId: appStore.activeStoreId ?? undefined,
            status: filter.status || undefined,
            from: filter.range?.[0],
            to: filter.range?.[1],
            page: filter.page,
            pageSize: filter.pageSize
        });
        rows.value = res.items;
        total.value = res.total;
    }
    finally {
        loading.value = false;
    }
}
async function loadTechList() {
    if (!appStore.activeStoreId)
        return;
    const r = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, page: 1, pageSize: 200 });
    techList.value = r.items;
}
async function submitCreate() {
    if (createForm.orderItemId <= 0) {
        ElMessage.warning('请填订单项 ID');
        return;
    }
    creating.value = true;
    try {
        await complaintsApi.create({
            orderItemId: createForm.orderItemId,
            tags: createForm.tags.trim() || null,
            comment: createForm.comment.trim() || null
        });
        ElMessage.success('已登记投诉');
        createForm.orderItemId = 0;
        createForm.tags = '';
        createForm.comment = '';
        reload();
    }
    finally {
        creating.value = false;
    }
}
function openResolve(row) {
    resolving.value = row;
    resolveForm.resolution = 'Reassigned';
    resolveForm.reassignedToTechnicianId = null;
    resolveForm.resolutionNote = '';
    resolveOpen.value = true;
}
async function submitResolve() {
    if (!resolving.value)
        return;
    if (resolveForm.resolution === 'Reassigned' && !resolveForm.reassignedToTechnicianId) {
        ElMessage.warning('请选择改派目标');
        return;
    }
    saving.value = true;
    try {
        await complaintsApi.resolve(resolving.value.id, {
            resolution: resolveForm.resolution,
            reassignedToTechnicianId: resolveForm.resolution === 'Reassigned' ? resolveForm.reassignedToTechnicianId : null,
            resolutionNote: resolveForm.resolutionNote.trim() || null
        });
        ElMessage.success('已处理');
        resolveOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
async function cancelOne(row) {
    await ElMessageBox.confirm(`确认取消订单 ${row.orderNo} 上的这条投诉记录？`, '提示', { type: 'warning' }).catch(() => null);
    await complaintsApi.cancel(row.id);
    ElMessage.success('已取消');
    reload();
}
watch(() => appStore.activeStoreId, () => {
    reload();
    loadTechList();
});
onMounted(async () => {
    await appStore.loadStores();
    await Promise.all([reload(), loadTechList()]);
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['resolve-detail']} */ ;
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
    role: "search",
});
const __VLS_4 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.filter.status),
}));
const __VLS_6 = __VLS_5({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.filter.status),
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onChange: (__VLS_ctx.reload)
};
__VLS_7.slots.default;
const __VLS_12 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    value: "Pending",
}));
const __VLS_14 = __VLS_13({
    value: "Pending",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
__VLS_15.slots.default;
var __VLS_15;
const __VLS_16 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    value: "Resolved",
}));
const __VLS_18 = __VLS_17({
    value: "Resolved",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
__VLS_19.slots.default;
var __VLS_19;
const __VLS_20 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    value: "Cancelled",
}));
const __VLS_22 = __VLS_21({
    value: "Cancelled",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
__VLS_23.slots.default;
var __VLS_23;
const __VLS_24 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    value: "",
}));
const __VLS_26 = __VLS_25({
    value: "",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
__VLS_27.slots.default;
var __VLS_27;
var __VLS_7;
const __VLS_28 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.filter.range),
    type: "daterange",
    valueFormat: "YYYY-MM-DD",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    ...{ style: {} },
}));
const __VLS_30 = __VLS_29({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.filter.range),
    type: "daterange",
    valueFormat: "YYYY-MM-DD",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onChange: (__VLS_ctx.reload)
};
var __VLS_31;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_36 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_38 = __VLS_37({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
let __VLS_40;
let __VLS_41;
let __VLS_42;
const __VLS_43 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_39.slots.default;
var __VLS_39;
const __VLS_44 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_46 = __VLS_45({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_47.slots.default;
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    prop: "orderNo",
    label: "订单号",
    width: "160",
}));
const __VLS_50 = __VLS_49({
    prop: "orderNo",
    label: "订单号",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "serviceName",
    label: "项目",
    width: "160",
}));
const __VLS_54 = __VLS_53({
    prop: "serviceName",
    label: "项目",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "originalTechnicianName",
    label: "被投诉技师",
    width: "120",
}));
const __VLS_58 = __VLS_57({
    prop: "originalTechnicianName",
    label: "被投诉技师",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    prop: "memberName",
    label: "会员",
    width: "120",
}));
const __VLS_62 = __VLS_61({
    prop: "memberName",
    label: "会员",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    label: "标签 / 描述",
    minWidth: "240",
}));
const __VLS_66 = __VLS_65({
    label: "标签 / 描述",
    minWidth: "240",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_67.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_67.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.tags) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
        for (const [t] of __VLS_getVForSourceType((row.tags.split(',')))) {
            const __VLS_68 = {}.ElTag;
            /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
            // @ts-ignore
            const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
                key: (t),
                size: "small",
                type: "warning",
                ...{ style: {} },
            }));
            const __VLS_70 = __VLS_69({
                key: (t),
                size: "small",
                type: "warning",
                ...{ style: {} },
            }, ...__VLS_functionalComponentArgsRest(__VLS_69));
            __VLS_71.slots.default;
            (t);
            var __VLS_71;
        }
    }
    if (row.comment) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "muted" },
        });
        (row.comment);
    }
}
var __VLS_67;
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
        type: (__VLS_ctx.statusTag(row.status)),
    }));
    const __VLS_78 = __VLS_77({
        type: (__VLS_ctx.statusTag(row.status)),
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
    label: "处理结果",
    width: "160",
}));
const __VLS_82 = __VLS_81({
    label: "处理结果",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
__VLS_83.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_83.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.resolution) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (__VLS_ctx.resolutionLabel(row.resolution));
        if (row.reassignedToTechnicianName) {
            __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                ...{ class: "muted" },
            });
            (row.reassignedToTechnicianName);
        }
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "muted" },
        });
    }
}
var __VLS_83;
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    label: "登记/处理",
    width: "160",
}));
const __VLS_86 = __VLS_85({
    label: "登记/处理",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    (row.recordedByName || '—');
    if (row.resolvedByName) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "muted" },
        });
        (row.resolvedByName);
        (__VLS_ctx.formatTime(row.resolvedAt));
    }
}
var __VLS_87;
const __VLS_88 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    label: "时间",
    width: "160",
}));
const __VLS_90 = __VLS_89({
    label: "时间",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
__VLS_91.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_91.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.formatTime(row.createdAt));
}
var __VLS_91;
const __VLS_92 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    label: "操作",
    width: "200",
    fixed: "right",
}));
const __VLS_94 = __VLS_93({
    label: "操作",
    width: "200",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
__VLS_95.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_95.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.status === 'Pending') {
        const __VLS_96 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
            ...{ 'onClick': {} },
            link: true,
            type: "primary",
        }));
        const __VLS_98 = __VLS_97({
            ...{ 'onClick': {} },
            link: true,
            type: "primary",
        }, ...__VLS_functionalComponentArgsRest(__VLS_97));
        let __VLS_100;
        let __VLS_101;
        let __VLS_102;
        const __VLS_103 = {
            onClick: (...[$event]) => {
                if (!(row.status === 'Pending'))
                    return;
                __VLS_ctx.openResolve(row);
            }
        };
        __VLS_99.slots.default;
        var __VLS_99;
    }
    if (row.status === 'Pending') {
        const __VLS_104 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }));
        const __VLS_106 = __VLS_105({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }, ...__VLS_functionalComponentArgsRest(__VLS_105));
        let __VLS_108;
        let __VLS_109;
        let __VLS_110;
        const __VLS_111 = {
            onClick: (...[$event]) => {
                if (!(row.status === 'Pending'))
                    return;
                __VLS_ctx.cancelOne(row);
            }
        };
        __VLS_107.slots.default;
        var __VLS_107;
    }
}
var __VLS_95;
var __VLS_47;
const __VLS_112 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.filter.page),
    pageSize: (__VLS_ctx.filter.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}));
const __VLS_114 = __VLS_113({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.filter.page),
    pageSize: (__VLS_ctx.filter.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
let __VLS_116;
let __VLS_117;
let __VLS_118;
const __VLS_119 = {
    onCurrentChange: ((p) => { __VLS_ctx.filter.page = p; __VLS_ctx.reload(); })
};
const __VLS_120 = {
    onSizeChange: ((s) => { __VLS_ctx.filter.pageSize = s; __VLS_ctx.filter.page = 1; __VLS_ctx.reload(); })
};
var __VLS_115;
var __VLS_3;
const __VLS_121 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_122 = __VLS_asFunctionalComponent(__VLS_121, new __VLS_121({
    shadow: "never",
    ...{ style: {} },
}));
const __VLS_123 = __VLS_122({
    shadow: "never",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_122));
__VLS_124.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_124.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "ck-header" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "muted" },
    });
}
const __VLS_125 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_126 = __VLS_asFunctionalComponent(__VLS_125, new __VLS_125({
    model: (__VLS_ctx.createForm),
    inline: true,
}));
const __VLS_127 = __VLS_126({
    model: (__VLS_ctx.createForm),
    inline: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_126));
__VLS_128.slots.default;
const __VLS_129 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_130 = __VLS_asFunctionalComponent(__VLS_129, new __VLS_129({
    label: "订单项 ID",
}));
const __VLS_131 = __VLS_130({
    label: "订单项 ID",
}, ...__VLS_functionalComponentArgsRest(__VLS_130));
__VLS_132.slots.default;
const __VLS_133 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_134 = __VLS_asFunctionalComponent(__VLS_133, new __VLS_133({
    modelValue: (__VLS_ctx.createForm.orderItemId),
    min: (1),
}));
const __VLS_135 = __VLS_134({
    modelValue: (__VLS_ctx.createForm.orderItemId),
    min: (1),
}, ...__VLS_functionalComponentArgsRest(__VLS_134));
var __VLS_132;
const __VLS_137 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_138 = __VLS_asFunctionalComponent(__VLS_137, new __VLS_137({
    label: "标签",
}));
const __VLS_139 = __VLS_138({
    label: "标签",
}, ...__VLS_functionalComponentArgsRest(__VLS_138));
__VLS_140.slots.default;
const __VLS_141 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_142 = __VLS_asFunctionalComponent(__VLS_141, new __VLS_141({
    modelValue: (__VLS_ctx.createForm.tags),
    placeholder: "逗号分隔，如：态度差,力度不合适",
    ...{ style: {} },
}));
const __VLS_143 = __VLS_142({
    modelValue: (__VLS_ctx.createForm.tags),
    placeholder: "逗号分隔，如：态度差,力度不合适",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_142));
var __VLS_140;
const __VLS_145 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_146 = __VLS_asFunctionalComponent(__VLS_145, new __VLS_145({
    label: "描述",
}));
const __VLS_147 = __VLS_146({
    label: "描述",
}, ...__VLS_functionalComponentArgsRest(__VLS_146));
__VLS_148.slots.default;
const __VLS_149 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_150 = __VLS_asFunctionalComponent(__VLS_149, new __VLS_149({
    modelValue: (__VLS_ctx.createForm.comment),
    placeholder: "客户原话/补充",
    ...{ style: {} },
}));
const __VLS_151 = __VLS_150({
    modelValue: (__VLS_ctx.createForm.comment),
    placeholder: "客户原话/补充",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_150));
var __VLS_148;
const __VLS_153 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_154 = __VLS_asFunctionalComponent(__VLS_153, new __VLS_153({}));
const __VLS_155 = __VLS_154({}, ...__VLS_functionalComponentArgsRest(__VLS_154));
__VLS_156.slots.default;
const __VLS_157 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_158 = __VLS_asFunctionalComponent(__VLS_157, new __VLS_157({
    ...{ 'onClick': {} },
    type: "primary",
    loading: (__VLS_ctx.creating),
}));
const __VLS_159 = __VLS_158({
    ...{ 'onClick': {} },
    type: "primary",
    loading: (__VLS_ctx.creating),
}, ...__VLS_functionalComponentArgsRest(__VLS_158));
let __VLS_161;
let __VLS_162;
let __VLS_163;
const __VLS_164 = {
    onClick: (__VLS_ctx.submitCreate)
};
__VLS_160.slots.default;
var __VLS_160;
var __VLS_156;
var __VLS_128;
var __VLS_124;
const __VLS_165 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_166 = __VLS_asFunctionalComponent(__VLS_165, new __VLS_165({
    modelValue: (__VLS_ctx.resolveOpen),
    title: (`处理投诉 #${__VLS_ctx.resolving?.id}`),
    width: "480px",
}));
const __VLS_167 = __VLS_166({
    modelValue: (__VLS_ctx.resolveOpen),
    title: (`处理投诉 #${__VLS_ctx.resolving?.id}`),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_166));
__VLS_168.slots.default;
if (__VLS_ctx.resolving) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "resolve-detail" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.resolving.serviceName);
    (__VLS_ctx.resolving.originalTechnicianName);
    if (__VLS_ctx.resolving.tags) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
            ...{ class: "muted" },
        });
        (__VLS_ctx.resolving.tags);
    }
    if (__VLS_ctx.resolving.comment) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
            ...{ class: "muted" },
        });
        (__VLS_ctx.resolving.comment);
    }
}
const __VLS_169 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_170 = __VLS_asFunctionalComponent(__VLS_169, new __VLS_169({
    model: (__VLS_ctx.resolveForm),
    labelWidth: "100px",
}));
const __VLS_171 = __VLS_170({
    model: (__VLS_ctx.resolveForm),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_170));
__VLS_172.slots.default;
const __VLS_173 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_174 = __VLS_asFunctionalComponent(__VLS_173, new __VLS_173({
    label: "处理方式",
}));
const __VLS_175 = __VLS_174({
    label: "处理方式",
}, ...__VLS_functionalComponentArgsRest(__VLS_174));
__VLS_176.slots.default;
const __VLS_177 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_178 = __VLS_asFunctionalComponent(__VLS_177, new __VLS_177({
    modelValue: (__VLS_ctx.resolveForm.resolution),
}));
const __VLS_179 = __VLS_178({
    modelValue: (__VLS_ctx.resolveForm.resolution),
}, ...__VLS_functionalComponentArgsRest(__VLS_178));
__VLS_180.slots.default;
const __VLS_181 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_182 = __VLS_asFunctionalComponent(__VLS_181, new __VLS_181({
    value: "Reassigned",
}));
const __VLS_183 = __VLS_182({
    value: "Reassigned",
}, ...__VLS_functionalComponentArgsRest(__VLS_182));
__VLS_184.slots.default;
var __VLS_184;
const __VLS_185 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_186 = __VLS_asFunctionalComponent(__VLS_185, new __VLS_185({
    value: "Refunded",
}));
const __VLS_187 = __VLS_186({
    value: "Refunded",
}, ...__VLS_functionalComponentArgsRest(__VLS_186));
__VLS_188.slots.default;
var __VLS_188;
const __VLS_189 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_190 = __VLS_asFunctionalComponent(__VLS_189, new __VLS_189({
    value: "Apologized",
}));
const __VLS_191 = __VLS_190({
    value: "Apologized",
}, ...__VLS_functionalComponentArgsRest(__VLS_190));
__VLS_192.slots.default;
var __VLS_192;
const __VLS_193 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_194 = __VLS_asFunctionalComponent(__VLS_193, new __VLS_193({
    value: "NoAction",
}));
const __VLS_195 = __VLS_194({
    value: "NoAction",
}, ...__VLS_functionalComponentArgsRest(__VLS_194));
__VLS_196.slots.default;
var __VLS_196;
var __VLS_180;
var __VLS_176;
if (__VLS_ctx.resolveForm.resolution === 'Reassigned') {
    const __VLS_197 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_198 = __VLS_asFunctionalComponent(__VLS_197, new __VLS_197({
        label: "改派给",
    }));
    const __VLS_199 = __VLS_198({
        label: "改派给",
    }, ...__VLS_functionalComponentArgsRest(__VLS_198));
    __VLS_200.slots.default;
    const __VLS_201 = {}.ElSelect;
    /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
    // @ts-ignore
    const __VLS_202 = __VLS_asFunctionalComponent(__VLS_201, new __VLS_201({
        modelValue: (__VLS_ctx.resolveForm.reassignedToTechnicianId),
        placeholder: "请选择新技师",
        ...{ style: {} },
        filterable: true,
    }));
    const __VLS_203 = __VLS_202({
        modelValue: (__VLS_ctx.resolveForm.reassignedToTechnicianId),
        placeholder: "请选择新技师",
        ...{ style: {} },
        filterable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_202));
    __VLS_204.slots.default;
    for (const [s] of __VLS_getVForSourceType((__VLS_ctx.techList))) {
        const __VLS_205 = {}.ElOption;
        /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
        // @ts-ignore
        const __VLS_206 = __VLS_asFunctionalComponent(__VLS_205, new __VLS_205({
            key: (s.id),
            label: (`${s.realName || s.username}（工号 ${s.employeeNo ?? '—'}）`),
            value: (s.id),
            disabled: (s.id === __VLS_ctx.resolving?.originalTechnicianId),
        }));
        const __VLS_207 = __VLS_206({
            key: (s.id),
            label: (`${s.realName || s.username}（工号 ${s.employeeNo ?? '—'}）`),
            value: (s.id),
            disabled: (s.id === __VLS_ctx.resolving?.originalTechnicianId),
        }, ...__VLS_functionalComponentArgsRest(__VLS_206));
    }
    var __VLS_204;
    var __VLS_200;
}
const __VLS_209 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_210 = __VLS_asFunctionalComponent(__VLS_209, new __VLS_209({
    label: "备注",
}));
const __VLS_211 = __VLS_210({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_210));
__VLS_212.slots.default;
const __VLS_213 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_214 = __VLS_asFunctionalComponent(__VLS_213, new __VLS_213({
    modelValue: (__VLS_ctx.resolveForm.resolutionNote),
    type: "textarea",
    rows: (2),
    maxlength: "500",
}));
const __VLS_215 = __VLS_214({
    modelValue: (__VLS_ctx.resolveForm.resolutionNote),
    type: "textarea",
    rows: (2),
    maxlength: "500",
}, ...__VLS_functionalComponentArgsRest(__VLS_214));
var __VLS_212;
var __VLS_172;
{
    const { footer: __VLS_thisSlot } = __VLS_168.slots;
    const __VLS_217 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_218 = __VLS_asFunctionalComponent(__VLS_217, new __VLS_217({
        ...{ 'onClick': {} },
    }));
    const __VLS_219 = __VLS_218({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_218));
    let __VLS_221;
    let __VLS_222;
    let __VLS_223;
    const __VLS_224 = {
        onClick: (...[$event]) => {
            __VLS_ctx.resolveOpen = false;
        }
    };
    __VLS_220.slots.default;
    var __VLS_220;
    const __VLS_225 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_226 = __VLS_asFunctionalComponent(__VLS_225, new __VLS_225({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_227 = __VLS_226({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_226));
    let __VLS_229;
    let __VLS_230;
    let __VLS_231;
    const __VLS_232 = {
        onClick: (__VLS_ctx.submitResolve)
    };
    __VLS_228.slots.default;
    var __VLS_228;
}
var __VLS_168;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['ck-header']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['resolve-detail']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Refresh: Refresh,
            rows: rows,
            total: total,
            loading: loading,
            saving: saving,
            creating: creating,
            filter: filter,
            createForm: createForm,
            resolveOpen: resolveOpen,
            resolving: resolving,
            resolveForm: resolveForm,
            techList: techList,
            statusLabel: statusLabel,
            statusTag: statusTag,
            resolutionLabel: resolutionLabel,
            formatTime: formatTime,
            reload: reload,
            submitCreate: submitCreate,
            openResolve: openResolve,
            submitResolve: submitResolve,
            cancelOne: cancelOne,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
