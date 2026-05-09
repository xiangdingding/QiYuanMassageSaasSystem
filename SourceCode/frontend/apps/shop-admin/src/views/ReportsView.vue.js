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
            payMethodRows: payMethodRows,
            loadDaily: loadDaily,
            loadPerformance: loadPerformance,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
