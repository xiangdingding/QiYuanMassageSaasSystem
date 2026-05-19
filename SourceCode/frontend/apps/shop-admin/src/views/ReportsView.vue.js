import { computed, onMounted, ref } from 'vue';
import dayjs from 'dayjs';
import { ElMessage } from 'element-plus';
import { reportsApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const tab = ref('daily');
const dailyDate = ref(dayjs().format('YYYY-MM-DD'));
const daily = ref(null);
const perfRange = ref([
    dayjs().subtract(7, 'day').format('YYYY-MM-DD'),
    dayjs().format('YYYY-MM-DD')
]);
const perfRows = ref([]);
const perfLoading = ref(false);
const monthlyMonth = ref(dayjs().format('YYYY-MM'));
const monthly = ref(null);
const yearlyYear = ref(dayjs().format('YYYY'));
const yearly = ref(null);
const popRange = ref([
    dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
    dayjs().format('YYYY-MM-DD')
]);
const popularity = ref([]);
const popLoading = ref(false);
const flowRange = ref([
    dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
    dayjs().format('YYYY-MM-DD')
]);
const flow = ref([]);
const flowLoading = ref(false);
const memberAnalysis = ref(null);
const trendMonths = ref(6);
const serviceTrend = ref(null);
const trendLoading = ref(false);
const qualityRange = ref([
    dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
    dayjs().format('YYYY-MM-DD')
]);
const quality = ref([]);
const qualityLoading = ref(false);
function formatDate(s) { return dayjs(s).format('YYYY-MM-DD'); }
function formatMonth(s) { return dayjs(s).format('YYYY-MM'); }
const trendMonthHeaders = computed(() => {
    const first = serviceTrend.value?.services[0];
    if (!first)
        return [];
    return first.months.map((m) => `${m.year}-${String(m.month).padStart(2, '0')}`);
});
const payMethodRows = computed(() => {
    if (!daily.value)
        return [];
    return [
        { label: '现金', value: daily.value.cashAmount },
        { label: '会员卡', value: daily.value.memberCardAmount },
        { label: '微信', value: daily.value.wechatAmount },
        { label: '支付宝', value: daily.value.alipayAmount },
        { label: '银行卡', value: daily.value.bankCardAmount }
    ];
});
async function loadDaily() {
    if (!appStore.activeStoreId)
        return;
    daily.value = await reportsApi.daily(appStore.activeStoreId, dailyDate.value);
}
async function loadPerformance() {
    if (!appStore.activeStoreId)
        return;
    if (!perfRange.value || perfRange.value.length !== 2) {
        ElMessage.warning('请选择日期区间');
        return;
    }
    const [from, to] = perfRange.value;
    perfLoading.value = true;
    try {
        perfRows.value = await reportsApi.technicianPerformance(appStore.activeStoreId, `${from}T00:00:00Z`, `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`);
    }
    finally {
        perfLoading.value = false;
    }
}
async function loadMonthly() {
    if (!appStore.activeStoreId)
        return;
    const [y, m] = monthlyMonth.value.split('-').map(Number);
    monthly.value = await reportsApi.monthly(appStore.activeStoreId, y, m);
}
async function loadYearly() {
    if (!appStore.activeStoreId)
        return;
    yearly.value = await reportsApi.yearly(appStore.activeStoreId, Number(yearlyYear.value));
}
async function loadPopularity() {
    if (!appStore.activeStoreId)
        return;
    const [from, to] = popRange.value;
    popLoading.value = true;
    try {
        popularity.value = await reportsApi.servicePopularity(appStore.activeStoreId, `${from}T00:00:00Z`, `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`);
    }
    finally {
        popLoading.value = false;
    }
}
async function loadFlow() {
    if (!appStore.activeStoreId)
        return;
    const [from, to] = flowRange.value;
    flowLoading.value = true;
    try {
        flow.value = await reportsApi.customerFlow(appStore.activeStoreId, `${from}T00:00:00Z`, `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`);
    }
    finally {
        flowLoading.value = false;
    }
}
async function loadMemberAnalysis() {
    if (!appStore.activeStoreId)
        return;
    memberAnalysis.value = await reportsApi.memberAnalysis(appStore.activeStoreId);
}
async function loadServiceTrend() {
    if (!appStore.activeStoreId)
        return;
    trendLoading.value = true;
    try {
        serviceTrend.value = await reportsApi.serviceTrend(appStore.activeStoreId, trendMonths.value);
    }
    finally {
        trendLoading.value = false;
    }
}
async function loadQuality() {
    if (!appStore.activeStoreId)
        return;
    const [from, to] = qualityRange.value;
    qualityLoading.value = true;
    try {
        quality.value = await reportsApi.technicianQuality(appStore.activeStoreId, `${from}T00:00:00Z`, `${dayjs(to).add(1, 'day').format('YYYY-MM-DD')}T00:00:00Z`);
    }
    finally {
        qualityLoading.value = false;
    }
}
onMounted(async () => {
    await appStore.loadStores();
    await loadDaily();
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
const __VLS_0 = {}.ElTabs;
/** @type {[typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    modelValue: (__VLS_ctx.tab),
}));
const __VLS_2 = __VLS_1({
    modelValue: (__VLS_ctx.tab),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
const __VLS_4 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    label: "日报",
    name: "daily",
}));
const __VLS_6 = __VLS_5({
    label: "日报",
    name: "daily",
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
__VLS_7.slots.default;
const __VLS_8 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    shadow: "never",
}));
const __VLS_10 = __VLS_9({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
__VLS_11.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_12 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    modelValue: (__VLS_ctx.dailyDate),
    type: "date",
    placeholder: "选择日期",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
    clearable: (false),
}));
const __VLS_14 = __VLS_13({
    modelValue: (__VLS_ctx.dailyDate),
    type: "date",
    placeholder: "选择日期",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
    clearable: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
const __VLS_16 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_18 = __VLS_17({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
let __VLS_20;
let __VLS_21;
let __VLS_22;
const __VLS_23 = {
    onClick: (__VLS_ctx.loadDaily)
};
__VLS_19.slots.default;
var __VLS_19;
if (__VLS_ctx.daily) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "daily-grid" },
    });
    const __VLS_24 = {}.ElRow;
    /** @type {[typeof __VLS_components.ElRow, typeof __VLS_components.elRow, typeof __VLS_components.ElRow, typeof __VLS_components.elRow, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
        gutter: (16),
        ...{ style: {} },
    }));
    const __VLS_26 = __VLS_25({
        gutter: (16),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_25));
    __VLS_27.slots.default;
    const __VLS_28 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
        span: (6),
    }));
    const __VLS_30 = __VLS_29({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_29));
    __VLS_31.slots.default;
    const __VLS_32 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_34 = __VLS_33({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_33));
    __VLS_35.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.daily.revenue.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    (__VLS_ctx.daily.orderCount);
    var __VLS_35;
    var __VLS_31;
    const __VLS_36 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
        span: (6),
    }));
    const __VLS_38 = __VLS_37({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_37));
    __VLS_39.slots.default;
    const __VLS_40 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_42 = __VLS_41({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
    __VLS_43.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.daily.refundAmount.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    (__VLS_ctx.daily.refundCount);
    var __VLS_43;
    var __VLS_39;
    const __VLS_44 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
        span: (6),
    }));
    const __VLS_46 = __VLS_45({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_45));
    __VLS_47.slots.default;
    const __VLS_48 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_50 = __VLS_49({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.daily.memberRechargeAmount.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    (__VLS_ctx.daily.memberRechargeCount);
    var __VLS_51;
    var __VLS_47;
    const __VLS_52 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
        span: (6),
    }));
    const __VLS_54 = __VLS_53({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_53));
    __VLS_55.slots.default;
    const __VLS_56 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_58 = __VLS_57({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_57));
    __VLS_59.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    ((__VLS_ctx.daily.revenue - __VLS_ctx.daily.refundAmount).toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    var __VLS_59;
    var __VLS_55;
    var __VLS_27;
    const __VLS_60 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
        ...{ style: {} },
        shadow: "never",
    }));
    const __VLS_62 = __VLS_61({
        ...{ style: {} },
        shadow: "never",
    }, ...__VLS_functionalComponentArgsRest(__VLS_61));
    __VLS_63.slots.default;
    {
        const { header: __VLS_thisSlot } = __VLS_63.slots;
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    const __VLS_64 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        data: (__VLS_ctx.payMethodRows),
        size: "small",
    }));
    const __VLS_66 = __VLS_65({
        data: (__VLS_ctx.payMethodRows),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    __VLS_67.slots.default;
    const __VLS_68 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
        prop: "label",
        label: "支付方式",
    }));
    const __VLS_70 = __VLS_69({
        prop: "label",
        label: "支付方式",
    }, ...__VLS_functionalComponentArgsRest(__VLS_69));
    const __VLS_72 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
        label: "金额",
    }));
    const __VLS_74 = __VLS_73({
        label: "金额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_73));
    __VLS_75.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_75.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.value.toFixed(2));
    }
    var __VLS_75;
    const __VLS_76 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
        label: "占比",
    }));
    const __VLS_78 = __VLS_77({
        label: "占比",
    }, ...__VLS_functionalComponentArgsRest(__VLS_77));
    __VLS_79.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_79.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.daily.revenue > 0 ? ((row.value / __VLS_ctx.daily.revenue) * 100).toFixed(1) : 0);
    }
    var __VLS_79;
    var __VLS_67;
    var __VLS_63;
}
var __VLS_11;
var __VLS_7;
const __VLS_80 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    label: "技师业绩",
    name: "performance",
}));
const __VLS_82 = __VLS_81({
    label: "技师业绩",
    name: "performance",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
__VLS_83.slots.default;
const __VLS_84 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    shadow: "never",
}));
const __VLS_86 = __VLS_85({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_88 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    modelValue: (__VLS_ctx.perfRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始日期",
    endPlaceholder: "结束日期",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_90 = __VLS_89({
    modelValue: (__VLS_ctx.perfRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始日期",
    endPlaceholder: "结束日期",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
const __VLS_92 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_94 = __VLS_93({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
let __VLS_96;
let __VLS_97;
let __VLS_98;
const __VLS_99 = {
    onClick: (__VLS_ctx.loadPerformance)
};
__VLS_95.slots.default;
var __VLS_95;
const __VLS_100 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    data: (__VLS_ctx.perfRows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_102 = __VLS_101({
    data: (__VLS_ctx.perfRows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.perfLoading) }, null, null);
__VLS_103.slots.default;
const __VLS_104 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}));
const __VLS_106 = __VLS_105({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_105));
const __VLS_108 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
    prop: "technicianName",
    label: "姓名",
    minWidth: "120",
}));
const __VLS_110 = __VLS_109({
    prop: "technicianName",
    label: "姓名",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_109));
const __VLS_112 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    label: "服务次数",
    width: "100",
    prop: "orderItemCount",
}));
const __VLS_114 = __VLS_113({
    label: "服务次数",
    width: "100",
    prop: "orderItemCount",
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
const __VLS_116 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
    label: "服务金额",
}));
const __VLS_118 = __VLS_117({
    label: "服务金额",
}, ...__VLS_functionalComponentArgsRest(__VLS_117));
__VLS_119.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_119.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.totalServiceAmount.toFixed(2));
}
var __VLS_119;
const __VLS_120 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    label: "提成合计",
}));
const __VLS_122 = __VLS_121({
    label: "提成合计",
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_123.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
        ...{ style: {} },
    });
    (row.totalCommission.toFixed(2));
}
var __VLS_123;
const __VLS_124 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    label: "服务时长",
}));
const __VLS_126 = __VLS_125({
    label: "服务时长",
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
__VLS_127.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_127.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (Math.round(row.totalDurationMinutes / 60 * 10) / 10);
}
var __VLS_127;
var __VLS_103;
var __VLS_87;
var __VLS_83;
const __VLS_128 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    label: "月报",
    name: "monthly",
}));
const __VLS_130 = __VLS_129({
    label: "月报",
    name: "monthly",
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_131.slots.default;
const __VLS_132 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    shadow: "never",
}));
const __VLS_134 = __VLS_133({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
__VLS_135.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_136 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
    modelValue: (__VLS_ctx.monthlyMonth),
    type: "month",
    placeholder: "选择月份",
    format: "YYYY-MM",
    valueFormat: "YYYY-MM",
    clearable: (false),
}));
const __VLS_138 = __VLS_137({
    modelValue: (__VLS_ctx.monthlyMonth),
    type: "month",
    placeholder: "选择月份",
    format: "YYYY-MM",
    valueFormat: "YYYY-MM",
    clearable: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_137));
const __VLS_140 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_142 = __VLS_141({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
let __VLS_144;
let __VLS_145;
let __VLS_146;
const __VLS_147 = {
    onClick: (__VLS_ctx.loadMonthly)
};
__VLS_143.slots.default;
var __VLS_143;
if (__VLS_ctx.monthly) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ style: {} },
    });
    const __VLS_148 = {}.ElRow;
    /** @type {[typeof __VLS_components.ElRow, typeof __VLS_components.elRow, typeof __VLS_components.ElRow, typeof __VLS_components.elRow, ]} */ ;
    // @ts-ignore
    const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
        gutter: (16),
    }));
    const __VLS_150 = __VLS_149({
        gutter: (16),
    }, ...__VLS_functionalComponentArgsRest(__VLS_149));
    __VLS_151.slots.default;
    const __VLS_152 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
        span: (6),
    }));
    const __VLS_154 = __VLS_153({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_153));
    __VLS_155.slots.default;
    const __VLS_156 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_158 = __VLS_157({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_157));
    __VLS_159.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.monthly.revenue.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    (__VLS_ctx.monthly.orderCount);
    var __VLS_159;
    var __VLS_155;
    const __VLS_160 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
        span: (6),
    }));
    const __VLS_162 = __VLS_161({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_161));
    __VLS_163.slots.default;
    const __VLS_164 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_166 = __VLS_165({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_165));
    __VLS_167.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.monthly.roundsCount);
    var __VLS_167;
    var __VLS_163;
    const __VLS_168 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
        span: (6),
    }));
    const __VLS_170 = __VLS_169({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_169));
    __VLS_171.slots.default;
    const __VLS_172 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_174 = __VLS_173({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_173));
    __VLS_175.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.monthly.rechargeAmount.toFixed(2));
    var __VLS_175;
    var __VLS_171;
    const __VLS_176 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
        span: (6),
    }));
    const __VLS_178 = __VLS_177({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_177));
    __VLS_179.slots.default;
    const __VLS_180 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_182 = __VLS_181({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_181));
    __VLS_183.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.monthly.averageOrder.toFixed(2));
    var __VLS_183;
    var __VLS_179;
    var __VLS_151;
    const __VLS_184 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
        ...{ style: {} },
        shadow: "never",
    }));
    const __VLS_186 = __VLS_185({
        ...{ style: {} },
        shadow: "never",
    }, ...__VLS_functionalComponentArgsRest(__VLS_185));
    __VLS_187.slots.default;
    {
        const { header: __VLS_thisSlot } = __VLS_187.slots;
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    const __VLS_188 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_189 = __VLS_asFunctionalComponent(__VLS_188, new __VLS_188({
        data: (__VLS_ctx.monthly.daily),
        size: "small",
    }));
    const __VLS_190 = __VLS_189({
        data: (__VLS_ctx.monthly.daily),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_189));
    __VLS_191.slots.default;
    const __VLS_192 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_193 = __VLS_asFunctionalComponent(__VLS_192, new __VLS_192({
        label: "日期",
    }));
    const __VLS_194 = __VLS_193({
        label: "日期",
    }, ...__VLS_functionalComponentArgsRest(__VLS_193));
    __VLS_195.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_195.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.formatDate(row.day));
    }
    var __VLS_195;
    const __VLS_196 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_197 = __VLS_asFunctionalComponent(__VLS_196, new __VLS_196({
        prop: "orderCount",
        label: "订单",
        width: "100",
    }));
    const __VLS_198 = __VLS_197({
        prop: "orderCount",
        label: "订单",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_197));
    const __VLS_200 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_201 = __VLS_asFunctionalComponent(__VLS_200, new __VLS_200({
        label: "营业额",
    }));
    const __VLS_202 = __VLS_201({
        label: "营业额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_201));
    __VLS_203.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_203.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.revenue.toFixed(2));
    }
    var __VLS_203;
    const __VLS_204 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_205 = __VLS_asFunctionalComponent(__VLS_204, new __VLS_204({
        prop: "rounds",
        label: "钟数",
        width: "100",
    }));
    const __VLS_206 = __VLS_205({
        prop: "rounds",
        label: "钟数",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_205));
    var __VLS_191;
    var __VLS_187;
}
var __VLS_135;
var __VLS_131;
const __VLS_208 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_209 = __VLS_asFunctionalComponent(__VLS_208, new __VLS_208({
    label: "年报",
    name: "yearly",
}));
const __VLS_210 = __VLS_209({
    label: "年报",
    name: "yearly",
}, ...__VLS_functionalComponentArgsRest(__VLS_209));
__VLS_211.slots.default;
const __VLS_212 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_213 = __VLS_asFunctionalComponent(__VLS_212, new __VLS_212({
    shadow: "never",
}));
const __VLS_214 = __VLS_213({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_213));
__VLS_215.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_216 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_217 = __VLS_asFunctionalComponent(__VLS_216, new __VLS_216({
    modelValue: (__VLS_ctx.yearlyYear),
    type: "year",
    placeholder: "选择年份",
    format: "YYYY",
    valueFormat: "YYYY",
    clearable: (false),
}));
const __VLS_218 = __VLS_217({
    modelValue: (__VLS_ctx.yearlyYear),
    type: "year",
    placeholder: "选择年份",
    format: "YYYY",
    valueFormat: "YYYY",
    clearable: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_217));
const __VLS_220 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_221 = __VLS_asFunctionalComponent(__VLS_220, new __VLS_220({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_222 = __VLS_221({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_221));
let __VLS_224;
let __VLS_225;
let __VLS_226;
const __VLS_227 = {
    onClick: (__VLS_ctx.loadYearly)
};
__VLS_223.slots.default;
var __VLS_223;
if (__VLS_ctx.yearly) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ style: {} },
    });
    const __VLS_228 = {}.ElRow;
    /** @type {[typeof __VLS_components.ElRow, typeof __VLS_components.elRow, typeof __VLS_components.ElRow, typeof __VLS_components.elRow, ]} */ ;
    // @ts-ignore
    const __VLS_229 = __VLS_asFunctionalComponent(__VLS_228, new __VLS_228({
        gutter: (16),
    }));
    const __VLS_230 = __VLS_229({
        gutter: (16),
    }, ...__VLS_functionalComponentArgsRest(__VLS_229));
    __VLS_231.slots.default;
    const __VLS_232 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_233 = __VLS_asFunctionalComponent(__VLS_232, new __VLS_232({
        span: (8),
    }));
    const __VLS_234 = __VLS_233({
        span: (8),
    }, ...__VLS_functionalComponentArgsRest(__VLS_233));
    __VLS_235.slots.default;
    const __VLS_236 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_237 = __VLS_asFunctionalComponent(__VLS_236, new __VLS_236({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_238 = __VLS_237({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_237));
    __VLS_239.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    (__VLS_ctx.yearly.year);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.yearly.revenue.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    (__VLS_ctx.yearly.orderCount);
    var __VLS_239;
    var __VLS_235;
    const __VLS_240 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_241 = __VLS_asFunctionalComponent(__VLS_240, new __VLS_240({
        span: (8),
    }));
    const __VLS_242 = __VLS_241({
        span: (8),
    }, ...__VLS_functionalComponentArgsRest(__VLS_241));
    __VLS_243.slots.default;
    const __VLS_244 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_245 = __VLS_asFunctionalComponent(__VLS_244, new __VLS_244({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_246 = __VLS_245({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_245));
    __VLS_247.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.yearly.roundsCount);
    var __VLS_247;
    var __VLS_243;
    const __VLS_248 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_249 = __VLS_asFunctionalComponent(__VLS_248, new __VLS_248({
        span: (8),
    }));
    const __VLS_250 = __VLS_249({
        span: (8),
    }, ...__VLS_functionalComponentArgsRest(__VLS_249));
    __VLS_251.slots.default;
    const __VLS_252 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_253 = __VLS_asFunctionalComponent(__VLS_252, new __VLS_252({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_254 = __VLS_253({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_253));
    __VLS_255.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    ((__VLS_ctx.yearly.revenue / Math.max(1, __VLS_ctx.yearly.monthly.length)).toFixed(2));
    var __VLS_255;
    var __VLS_251;
    var __VLS_231;
    const __VLS_256 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_257 = __VLS_asFunctionalComponent(__VLS_256, new __VLS_256({
        ...{ style: {} },
        shadow: "never",
    }));
    const __VLS_258 = __VLS_257({
        ...{ style: {} },
        shadow: "never",
    }, ...__VLS_functionalComponentArgsRest(__VLS_257));
    __VLS_259.slots.default;
    {
        const { header: __VLS_thisSlot } = __VLS_259.slots;
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    const __VLS_260 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_261 = __VLS_asFunctionalComponent(__VLS_260, new __VLS_260({
        data: (__VLS_ctx.yearly.monthly),
        size: "small",
    }));
    const __VLS_262 = __VLS_261({
        data: (__VLS_ctx.yearly.monthly),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_261));
    __VLS_263.slots.default;
    const __VLS_264 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_265 = __VLS_asFunctionalComponent(__VLS_264, new __VLS_264({
        label: "月份",
    }));
    const __VLS_266 = __VLS_265({
        label: "月份",
    }, ...__VLS_functionalComponentArgsRest(__VLS_265));
    __VLS_267.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_267.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.formatMonth(row.day));
    }
    var __VLS_267;
    const __VLS_268 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_269 = __VLS_asFunctionalComponent(__VLS_268, new __VLS_268({
        prop: "orderCount",
        label: "订单",
        width: "100",
    }));
    const __VLS_270 = __VLS_269({
        prop: "orderCount",
        label: "订单",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_269));
    const __VLS_272 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_273 = __VLS_asFunctionalComponent(__VLS_272, new __VLS_272({
        label: "营业额",
    }));
    const __VLS_274 = __VLS_273({
        label: "营业额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_273));
    __VLS_275.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_275.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.revenue.toFixed(2));
    }
    var __VLS_275;
    const __VLS_276 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_277 = __VLS_asFunctionalComponent(__VLS_276, new __VLS_276({
        prop: "rounds",
        label: "钟数",
        width: "100",
    }));
    const __VLS_278 = __VLS_277({
        prop: "rounds",
        label: "钟数",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_277));
    var __VLS_263;
    var __VLS_259;
}
var __VLS_215;
var __VLS_211;
const __VLS_280 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_281 = __VLS_asFunctionalComponent(__VLS_280, new __VLS_280({
    label: "服务热度",
    name: "popularity",
}));
const __VLS_282 = __VLS_281({
    label: "服务热度",
    name: "popularity",
}, ...__VLS_functionalComponentArgsRest(__VLS_281));
__VLS_283.slots.default;
const __VLS_284 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_285 = __VLS_asFunctionalComponent(__VLS_284, new __VLS_284({
    shadow: "never",
}));
const __VLS_286 = __VLS_285({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_285));
__VLS_287.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_288 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_289 = __VLS_asFunctionalComponent(__VLS_288, new __VLS_288({
    modelValue: (__VLS_ctx.popRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_290 = __VLS_289({
    modelValue: (__VLS_ctx.popRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_289));
const __VLS_292 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_293 = __VLS_asFunctionalComponent(__VLS_292, new __VLS_292({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_294 = __VLS_293({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_293));
let __VLS_296;
let __VLS_297;
let __VLS_298;
const __VLS_299 = {
    onClick: (__VLS_ctx.loadPopularity)
};
__VLS_295.slots.default;
var __VLS_295;
const __VLS_300 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_301 = __VLS_asFunctionalComponent(__VLS_300, new __VLS_300({
    data: (__VLS_ctx.popularity),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_302 = __VLS_301({
    data: (__VLS_ctx.popularity),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_301));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.popLoading) }, null, null);
__VLS_303.slots.default;
const __VLS_304 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_305 = __VLS_asFunctionalComponent(__VLS_304, new __VLS_304({
    prop: "serviceName",
    label: "服务",
    minWidth: "160",
}));
const __VLS_306 = __VLS_305({
    prop: "serviceName",
    label: "服务",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_305));
const __VLS_308 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_309 = __VLS_asFunctionalComponent(__VLS_308, new __VLS_308({
    prop: "orderItemCount",
    label: "下单次数",
    width: "120",
}));
const __VLS_310 = __VLS_309({
    prop: "orderItemCount",
    label: "下单次数",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_309));
const __VLS_312 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_313 = __VLS_asFunctionalComponent(__VLS_312, new __VLS_312({
    prop: "roundsCount",
    label: "钟数合计",
    width: "120",
}));
const __VLS_314 = __VLS_313({
    prop: "roundsCount",
    label: "钟数合计",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_313));
const __VLS_316 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_317 = __VLS_asFunctionalComponent(__VLS_316, new __VLS_316({
    label: "营业额",
}));
const __VLS_318 = __VLS_317({
    label: "营业额",
}, ...__VLS_functionalComponentArgsRest(__VLS_317));
__VLS_319.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_319.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.revenue.toFixed(2));
}
var __VLS_319;
var __VLS_303;
var __VLS_287;
var __VLS_283;
const __VLS_320 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_321 = __VLS_asFunctionalComponent(__VLS_320, new __VLS_320({
    label: "客流",
    name: "flow",
}));
const __VLS_322 = __VLS_321({
    label: "客流",
    name: "flow",
}, ...__VLS_functionalComponentArgsRest(__VLS_321));
__VLS_323.slots.default;
const __VLS_324 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_325 = __VLS_asFunctionalComponent(__VLS_324, new __VLS_324({
    shadow: "never",
}));
const __VLS_326 = __VLS_325({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_325));
__VLS_327.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_328 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_329 = __VLS_asFunctionalComponent(__VLS_328, new __VLS_328({
    modelValue: (__VLS_ctx.flowRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_330 = __VLS_329({
    modelValue: (__VLS_ctx.flowRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_329));
const __VLS_332 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_333 = __VLS_asFunctionalComponent(__VLS_332, new __VLS_332({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_334 = __VLS_333({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_333));
let __VLS_336;
let __VLS_337;
let __VLS_338;
const __VLS_339 = {
    onClick: (__VLS_ctx.loadFlow)
};
__VLS_335.slots.default;
var __VLS_335;
const __VLS_340 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_341 = __VLS_asFunctionalComponent(__VLS_340, new __VLS_340({
    data: (__VLS_ctx.flow),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_342 = __VLS_341({
    data: (__VLS_ctx.flow),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_341));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.flowLoading) }, null, null);
__VLS_343.slots.default;
const __VLS_344 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_345 = __VLS_asFunctionalComponent(__VLS_344, new __VLS_344({
    label: "日期",
}));
const __VLS_346 = __VLS_345({
    label: "日期",
}, ...__VLS_functionalComponentArgsRest(__VLS_345));
__VLS_347.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_347.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.formatDate(row.date));
}
var __VLS_347;
const __VLS_348 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_349 = __VLS_asFunctionalComponent(__VLS_348, new __VLS_348({
    prop: "orderCount",
    label: "订单数",
    width: "120",
}));
const __VLS_350 = __VLS_349({
    prop: "orderCount",
    label: "订单数",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_349));
const __VLS_352 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_353 = __VLS_asFunctionalComponent(__VLS_352, new __VLS_352({
    prop: "uniqueMembers",
    label: "唯一会员",
    width: "120",
}));
const __VLS_354 = __VLS_353({
    prop: "uniqueMembers",
    label: "唯一会员",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_353));
var __VLS_343;
var __VLS_327;
var __VLS_323;
const __VLS_356 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_357 = __VLS_asFunctionalComponent(__VLS_356, new __VLS_356({
    label: "会员分析",
    name: "memberAnalysis",
}));
const __VLS_358 = __VLS_357({
    label: "会员分析",
    name: "memberAnalysis",
}, ...__VLS_functionalComponentArgsRest(__VLS_357));
__VLS_359.slots.default;
const __VLS_360 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_361 = __VLS_asFunctionalComponent(__VLS_360, new __VLS_360({
    shadow: "never",
}));
const __VLS_362 = __VLS_361({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_361));
__VLS_363.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_364 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_365 = __VLS_asFunctionalComponent(__VLS_364, new __VLS_364({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_366 = __VLS_365({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_365));
let __VLS_368;
let __VLS_369;
let __VLS_370;
const __VLS_371 = {
    onClick: (__VLS_ctx.loadMemberAnalysis)
};
__VLS_367.slots.default;
var __VLS_367;
if (__VLS_ctx.memberAnalysis) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ style: {} },
    });
    const __VLS_372 = {}.ElRow;
    /** @type {[typeof __VLS_components.ElRow, typeof __VLS_components.elRow, typeof __VLS_components.ElRow, typeof __VLS_components.elRow, ]} */ ;
    // @ts-ignore
    const __VLS_373 = __VLS_asFunctionalComponent(__VLS_372, new __VLS_372({
        gutter: (16),
    }));
    const __VLS_374 = __VLS_373({
        gutter: (16),
    }, ...__VLS_functionalComponentArgsRest(__VLS_373));
    __VLS_375.slots.default;
    const __VLS_376 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_377 = __VLS_asFunctionalComponent(__VLS_376, new __VLS_376({
        span: (6),
    }));
    const __VLS_378 = __VLS_377({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_377));
    __VLS_379.slots.default;
    const __VLS_380 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_381 = __VLS_asFunctionalComponent(__VLS_380, new __VLS_380({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_382 = __VLS_381({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_381));
    __VLS_383.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.totalMembers);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-sub" },
    });
    (__VLS_ctx.memberAnalysis.newMembersThisMonth);
    var __VLS_383;
    var __VLS_379;
    const __VLS_384 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_385 = __VLS_asFunctionalComponent(__VLS_384, new __VLS_384({
        span: (6),
    }));
    const __VLS_386 = __VLS_385({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_385));
    __VLS_387.slots.default;
    const __VLS_388 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_389 = __VLS_asFunctionalComponent(__VLS_388, new __VLS_388({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_390 = __VLS_389({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_389));
    __VLS_391.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.activeMembers);
    var __VLS_391;
    var __VLS_387;
    const __VLS_392 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_393 = __VLS_asFunctionalComponent(__VLS_392, new __VLS_392({
        span: (6),
    }));
    const __VLS_394 = __VLS_393({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_393));
    __VLS_395.slots.default;
    const __VLS_396 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_397 = __VLS_asFunctionalComponent(__VLS_396, new __VLS_396({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_398 = __VLS_397({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_397));
    __VLS_399.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.dormantMembers);
    var __VLS_399;
    var __VLS_395;
    const __VLS_400 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_401 = __VLS_asFunctionalComponent(__VLS_400, new __VLS_400({
        span: (6),
    }));
    const __VLS_402 = __VLS_401({
        span: (6),
    }, ...__VLS_functionalComponentArgsRest(__VLS_401));
    __VLS_403.slots.default;
    const __VLS_404 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_405 = __VLS_asFunctionalComponent(__VLS_404, new __VLS_404({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_406 = __VLS_405({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_405));
    __VLS_407.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.lostMembers);
    var __VLS_407;
    var __VLS_403;
    var __VLS_375;
    const __VLS_408 = {}.ElRow;
    /** @type {[typeof __VLS_components.ElRow, typeof __VLS_components.elRow, typeof __VLS_components.ElRow, typeof __VLS_components.elRow, ]} */ ;
    // @ts-ignore
    const __VLS_409 = __VLS_asFunctionalComponent(__VLS_408, new __VLS_408({
        gutter: (16),
        ...{ style: {} },
    }));
    const __VLS_410 = __VLS_409({
        gutter: (16),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_409));
    __VLS_411.slots.default;
    const __VLS_412 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_413 = __VLS_asFunctionalComponent(__VLS_412, new __VLS_412({
        span: (8),
    }));
    const __VLS_414 = __VLS_413({
        span: (8),
    }, ...__VLS_functionalComponentArgsRest(__VLS_413));
    __VLS_415.slots.default;
    const __VLS_416 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_417 = __VLS_asFunctionalComponent(__VLS_416, new __VLS_416({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_418 = __VLS_417({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_417));
    __VLS_419.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.neverConsumed);
    var __VLS_419;
    var __VLS_415;
    const __VLS_420 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_421 = __VLS_asFunctionalComponent(__VLS_420, new __VLS_420({
        span: (8),
    }));
    const __VLS_422 = __VLS_421({
        span: (8),
    }, ...__VLS_functionalComponentArgsRest(__VLS_421));
    __VLS_423.slots.default;
    const __VLS_424 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_425 = __VLS_asFunctionalComponent(__VLS_424, new __VLS_424({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_426 = __VLS_425({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_425));
    __VLS_427.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.repeatMembers);
    var __VLS_427;
    var __VLS_423;
    const __VLS_428 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_429 = __VLS_asFunctionalComponent(__VLS_428, new __VLS_428({
        span: (8),
    }));
    const __VLS_430 = __VLS_429({
        span: (8),
    }, ...__VLS_functionalComponentArgsRest(__VLS_429));
    __VLS_431.slots.default;
    const __VLS_432 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_433 = __VLS_asFunctionalComponent(__VLS_432, new __VLS_432({
        ...{ class: "metric" },
        shadow: "hover",
    }));
    const __VLS_434 = __VLS_433({
        ...{ class: "metric" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_433));
    __VLS_435.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-label" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-value" },
    });
    (__VLS_ctx.memberAnalysis.repeatRate);
    var __VLS_435;
    var __VLS_431;
    var __VLS_411;
}
var __VLS_363;
var __VLS_359;
const __VLS_436 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_437 = __VLS_asFunctionalComponent(__VLS_436, new __VLS_436({
    label: "服务趋势",
    name: "serviceTrend",
}));
const __VLS_438 = __VLS_437({
    label: "服务趋势",
    name: "serviceTrend",
}, ...__VLS_functionalComponentArgsRest(__VLS_437));
__VLS_439.slots.default;
const __VLS_440 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_441 = __VLS_asFunctionalComponent(__VLS_440, new __VLS_440({
    shadow: "never",
}));
const __VLS_442 = __VLS_441({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_441));
__VLS_443.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_444 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_445 = __VLS_asFunctionalComponent(__VLS_444, new __VLS_444({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.trendMonths),
    ...{ style: {} },
}));
const __VLS_446 = __VLS_445({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.trendMonths),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_445));
let __VLS_448;
let __VLS_449;
let __VLS_450;
const __VLS_451 = {
    onChange: (__VLS_ctx.loadServiceTrend)
};
__VLS_447.slots.default;
const __VLS_452 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_453 = __VLS_asFunctionalComponent(__VLS_452, new __VLS_452({
    value: (6),
    label: "近 6 个月",
}));
const __VLS_454 = __VLS_453({
    value: (6),
    label: "近 6 个月",
}, ...__VLS_functionalComponentArgsRest(__VLS_453));
const __VLS_456 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_457 = __VLS_asFunctionalComponent(__VLS_456, new __VLS_456({
    value: (12),
    label: "近 12 个月",
}));
const __VLS_458 = __VLS_457({
    value: (12),
    label: "近 12 个月",
}, ...__VLS_functionalComponentArgsRest(__VLS_457));
var __VLS_447;
const __VLS_460 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_461 = __VLS_asFunctionalComponent(__VLS_460, new __VLS_460({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_462 = __VLS_461({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_461));
let __VLS_464;
let __VLS_465;
let __VLS_466;
const __VLS_467 = {
    onClick: (__VLS_ctx.loadServiceTrend)
};
__VLS_463.slots.default;
var __VLS_463;
const __VLS_468 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_469 = __VLS_asFunctionalComponent(__VLS_468, new __VLS_468({
    data: (__VLS_ctx.serviceTrend?.services ?? []),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_470 = __VLS_469({
    data: (__VLS_ctx.serviceTrend?.services ?? []),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_469));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.trendLoading) }, null, null);
__VLS_471.slots.default;
const __VLS_472 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_473 = __VLS_asFunctionalComponent(__VLS_472, new __VLS_472({
    prop: "serviceName",
    label: "服务",
    minWidth: "140",
    fixed: true,
}));
const __VLS_474 = __VLS_473({
    prop: "serviceName",
    label: "服务",
    minWidth: "140",
    fixed: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_473));
const __VLS_476 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_477 = __VLS_asFunctionalComponent(__VLS_476, new __VLS_476({
    prop: "totalRounds",
    label: "总钟数",
    width: "90",
}));
const __VLS_478 = __VLS_477({
    prop: "totalRounds",
    label: "总钟数",
    width: "90",
}, ...__VLS_functionalComponentArgsRest(__VLS_477));
for (const [label, idx] of __VLS_getVForSourceType((__VLS_ctx.trendMonthHeaders))) {
    const __VLS_480 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_481 = __VLS_asFunctionalComponent(__VLS_480, new __VLS_480({
        key: (idx),
        label: (label),
        width: "88",
    }));
    const __VLS_482 = __VLS_481({
        key: (idx),
        label: (label),
        width: "88",
    }, ...__VLS_functionalComponentArgsRest(__VLS_481));
    __VLS_483.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_483.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.months[idx]?.rounds ?? 0);
    }
    var __VLS_483;
}
var __VLS_471;
var __VLS_443;
var __VLS_439;
const __VLS_484 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_485 = __VLS_asFunctionalComponent(__VLS_484, new __VLS_484({
    label: "技师质量",
    name: "quality",
}));
const __VLS_486 = __VLS_485({
    label: "技师质量",
    name: "quality",
}, ...__VLS_functionalComponentArgsRest(__VLS_485));
__VLS_487.slots.default;
const __VLS_488 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_489 = __VLS_asFunctionalComponent(__VLS_488, new __VLS_488({
    shadow: "never",
}));
const __VLS_490 = __VLS_489({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_489));
__VLS_491.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_492 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_493 = __VLS_asFunctionalComponent(__VLS_492, new __VLS_492({
    modelValue: (__VLS_ctx.qualityRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_494 = __VLS_493({
    modelValue: (__VLS_ctx.qualityRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_493));
const __VLS_496 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_497 = __VLS_asFunctionalComponent(__VLS_496, new __VLS_496({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_498 = __VLS_497({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_497));
let __VLS_500;
let __VLS_501;
let __VLS_502;
const __VLS_503 = {
    onClick: (__VLS_ctx.loadQuality)
};
__VLS_499.slots.default;
var __VLS_499;
const __VLS_504 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_505 = __VLS_asFunctionalComponent(__VLS_504, new __VLS_504({
    data: (__VLS_ctx.quality),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_506 = __VLS_505({
    data: (__VLS_ctx.quality),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_505));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.qualityLoading) }, null, null);
__VLS_507.slots.default;
const __VLS_508 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_509 = __VLS_asFunctionalComponent(__VLS_508, new __VLS_508({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}));
const __VLS_510 = __VLS_509({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_509));
const __VLS_512 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_513 = __VLS_asFunctionalComponent(__VLS_512, new __VLS_512({
    prop: "technicianName",
    label: "技师",
    minWidth: "120",
}));
const __VLS_514 = __VLS_513({
    prop: "technicianName",
    label: "技师",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_513));
const __VLS_516 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_517 = __VLS_asFunctionalComponent(__VLS_516, new __VLS_516({
    prop: "roundCount",
    label: "钟数",
    width: "100",
}));
const __VLS_518 = __VLS_517({
    prop: "roundCount",
    label: "钟数",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_517));
const __VLS_520 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_521 = __VLS_asFunctionalComponent(__VLS_520, new __VLS_520({
    prop: "complaintCount",
    label: "投诉数",
    width: "100",
}));
const __VLS_522 = __VLS_521({
    prop: "complaintCount",
    label: "投诉数",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_521));
const __VLS_524 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_525 = __VLS_asFunctionalComponent(__VLS_524, new __VLS_524({
    label: "投诉率",
    width: "120",
}));
const __VLS_526 = __VLS_525({
    label: "投诉率",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_525));
__VLS_527.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_527.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ style: ({ color: row.complaintRate > 5 ? '#d9534f' : '#2d6a4f', fontWeight: 600 }) },
    });
    (row.complaintRate);
}
var __VLS_527;
var __VLS_507;
var __VLS_491;
var __VLS_487;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['daily-grid']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['m-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['metric']} */ ;
/** @type {__VLS_StyleScopedClasses['m-label']} */ ;
/** @type {__VLS_StyleScopedClasses['m-value']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            tab: tab,
            dailyDate: dailyDate,
            daily: daily,
            perfRange: perfRange,
            perfRows: perfRows,
            perfLoading: perfLoading,
            monthlyMonth: monthlyMonth,
            monthly: monthly,
            yearlyYear: yearlyYear,
            yearly: yearly,
            popRange: popRange,
            popularity: popularity,
            popLoading: popLoading,
            flowRange: flowRange,
            flow: flow,
            flowLoading: flowLoading,
            memberAnalysis: memberAnalysis,
            trendMonths: trendMonths,
            serviceTrend: serviceTrend,
            trendLoading: trendLoading,
            qualityRange: qualityRange,
            quality: quality,
            qualityLoading: qualityLoading,
            formatDate: formatDate,
            formatMonth: formatMonth,
            trendMonthHeaders: trendMonthHeaders,
            payMethodRows: payMethodRows,
            loadDaily: loadDaily,
            loadPerformance: loadPerformance,
            loadMonthly: loadMonthly,
            loadYearly: loadYearly,
            loadPopularity: loadPopularity,
            loadFlow: loadFlow,
            loadMemberAnalysis: loadMemberAnalysis,
            loadServiceTrend: loadServiceTrend,
            loadQuality: loadQuality,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
