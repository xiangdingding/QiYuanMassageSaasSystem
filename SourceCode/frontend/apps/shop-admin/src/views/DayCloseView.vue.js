import { computed, onMounted, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { dayClosesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const businessDate = ref(dayjs().format('YYYY-MM-DD'));
const preview = ref(null);
const actualCash = ref(0);
const remark = ref('');
const submitting = ref(false);
const history = ref([]);
const variance = computed(() => actualCash.value - (preview.value?.expectedCash ?? 0));
const varianceClass = computed(() => {
    if (variance.value === 0)
        return 'ok';
    return variance.value < 0 ? 'short' : 'over';
});
function disableFuture(d) {
    return d.getTime() > Date.now();
}
async function loadPreview() {
    if (!appStore.activeStoreId)
        return;
    preview.value = await dayClosesApi.preview(appStore.activeStoreId, businessDate.value);
    actualCash.value = preview.value.expectedCash;
    remark.value = '';
}
async function loadHistory() {
    if (!appStore.activeStoreId)
        return;
    history.value = await dayClosesApi.history(appStore.activeStoreId);
}
async function submit() {
    if (!appStore.activeStoreId || !preview.value)
        return;
    if (Math.abs(variance.value) > 0.005) {
        await ElMessageBox.confirm(`差额 ¥${variance.value.toFixed(2)}，确定提交？`, '差额确认', { type: 'warning' }).catch(() => null);
    }
    submitting.value = true;
    try {
        await dayClosesApi.submit({
            storeId: appStore.activeStoreId,
            businessDate: businessDate.value,
            actualCash: actualCash.value,
            remark: remark.value || null
        });
        ElMessage.success('日结已提交');
        await Promise.all([loadPreview(), loadHistory()]);
    }
    catch {
        /* http 已弹错 */
    }
    finally {
        submitting.value = false;
    }
}
watch(() => appStore.activeStoreId, () => {
    loadPreview();
    loadHistory();
});
onMounted(async () => {
    await appStore.loadStores();
    await Promise.all([loadPreview(), loadHistory()]);
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['variance']} */ ;
/** @type {__VLS_StyleScopedClasses['variance']} */ ;
/** @type {__VLS_StyleScopedClasses['variance']} */ ;
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "title" },
});
const __VLS_4 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.businessDate),
    type: "date",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
    disabledDate: (__VLS_ctx.disableFuture),
}));
const __VLS_6 = __VLS_5({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.businessDate),
    type: "date",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
    disabledDate: (__VLS_ctx.disableFuture),
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onChange: (__VLS_ctx.loadPreview)
};
var __VLS_7;
const __VLS_12 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_14 = __VLS_13({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
let __VLS_16;
let __VLS_17;
let __VLS_18;
const __VLS_19 = {
    onClick: (__VLS_ctx.loadPreview)
};
__VLS_15.slots.default;
var __VLS_15;
if (__VLS_ctx.preview) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "preview" },
    });
    if (__VLS_ctx.preview.alreadyClosed) {
        const __VLS_20 = {}.ElAlert;
        /** @type {[typeof __VLS_components.ElAlert, typeof __VLS_components.elAlert, typeof __VLS_components.ElAlert, typeof __VLS_components.elAlert, ]} */ ;
        // @ts-ignore
        const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
            type: "warning",
            closable: (false),
            showIcon: true,
        }));
        const __VLS_22 = __VLS_21({
            type: "warning",
            closable: (false),
            showIcon: true,
        }, ...__VLS_functionalComponentArgsRest(__VLS_21));
        __VLS_23.slots.default;
        var __VLS_23;
    }
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
    const __VLS_32 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
        title: "完成订单数",
        value: (__VLS_ctx.preview.orderCount),
    }));
    const __VLS_34 = __VLS_33({
        title: "完成订单数",
        value: (__VLS_ctx.preview.orderCount),
    }, ...__VLS_functionalComponentArgsRest(__VLS_33));
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
    const __VLS_40 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        title: "营业总额",
        value: (__VLS_ctx.preview.revenueTotal),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_42 = __VLS_41({
        title: "营业总额",
        value: (__VLS_ctx.preview.revenueTotal),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
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
    const __VLS_48 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
        title: "预期现金",
        value: (__VLS_ctx.preview.expectedCash),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_50 = __VLS_49({
        title: "预期现金",
        value: (__VLS_ctx.preview.expectedCash),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_49));
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
    const __VLS_56 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
        title: "充值入账",
        value: (__VLS_ctx.preview.rechargeAmount),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_58 = __VLS_57({
        title: "充值入账",
        value: (__VLS_ctx.preview.rechargeAmount),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_57));
    var __VLS_55;
    var __VLS_27;
    const __VLS_60 = {}.ElDivider;
    /** @type {[typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({}));
    const __VLS_62 = __VLS_61({}, ...__VLS_functionalComponentArgsRest(__VLS_61));
    __VLS_63.slots.default;
    var __VLS_63;
    const __VLS_64 = {}.ElRow;
    /** @type {[typeof __VLS_components.ElRow, typeof __VLS_components.elRow, typeof __VLS_components.ElRow, typeof __VLS_components.elRow, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        gutter: (16),
    }));
    const __VLS_66 = __VLS_65({
        gutter: (16),
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    __VLS_67.slots.default;
    const __VLS_68 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
        span: (4),
    }));
    const __VLS_70 = __VLS_69({
        span: (4),
    }, ...__VLS_functionalComponentArgsRest(__VLS_69));
    __VLS_71.slots.default;
    const __VLS_72 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
        title: "现金",
        value: (__VLS_ctx.preview.cashAmount),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_74 = __VLS_73({
        title: "现金",
        value: (__VLS_ctx.preview.cashAmount),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_73));
    var __VLS_71;
    const __VLS_76 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
        span: (4),
    }));
    const __VLS_78 = __VLS_77({
        span: (4),
    }, ...__VLS_functionalComponentArgsRest(__VLS_77));
    __VLS_79.slots.default;
    const __VLS_80 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
        title: "会员卡",
        value: (__VLS_ctx.preview.memberCardAmount),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_82 = __VLS_81({
        title: "会员卡",
        value: (__VLS_ctx.preview.memberCardAmount),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_81));
    var __VLS_79;
    const __VLS_84 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
        span: (4),
    }));
    const __VLS_86 = __VLS_85({
        span: (4),
    }, ...__VLS_functionalComponentArgsRest(__VLS_85));
    __VLS_87.slots.default;
    const __VLS_88 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
        title: "微信",
        value: (__VLS_ctx.preview.wechatAmount),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_90 = __VLS_89({
        title: "微信",
        value: (__VLS_ctx.preview.wechatAmount),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_89));
    var __VLS_87;
    const __VLS_92 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
        span: (4),
    }));
    const __VLS_94 = __VLS_93({
        span: (4),
    }, ...__VLS_functionalComponentArgsRest(__VLS_93));
    __VLS_95.slots.default;
    const __VLS_96 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
        title: "支付宝",
        value: (__VLS_ctx.preview.alipayAmount),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_98 = __VLS_97({
        title: "支付宝",
        value: (__VLS_ctx.preview.alipayAmount),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_97));
    var __VLS_95;
    const __VLS_100 = {}.ElCol;
    /** @type {[typeof __VLS_components.ElCol, typeof __VLS_components.elCol, typeof __VLS_components.ElCol, typeof __VLS_components.elCol, ]} */ ;
    // @ts-ignore
    const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
        span: (4),
    }));
    const __VLS_102 = __VLS_101({
        span: (4),
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
    __VLS_103.slots.default;
    const __VLS_104 = {}.ElStatistic;
    /** @type {[typeof __VLS_components.ElStatistic, typeof __VLS_components.elStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
        title: "银行卡",
        value: (__VLS_ctx.preview.bankCardAmount),
        precision: (2),
        prefix: "¥",
    }));
    const __VLS_106 = __VLS_105({
        title: "银行卡",
        value: (__VLS_ctx.preview.bankCardAmount),
        precision: (2),
        prefix: "¥",
    }, ...__VLS_functionalComponentArgsRest(__VLS_105));
    var __VLS_103;
    var __VLS_67;
    const __VLS_108 = {}.ElDivider;
    /** @type {[typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, ]} */ ;
    // @ts-ignore
    const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({}));
    const __VLS_110 = __VLS_109({}, ...__VLS_functionalComponentArgsRest(__VLS_109));
    __VLS_111.slots.default;
    var __VLS_111;
    const __VLS_112 = {}.ElForm;
    /** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
        labelWidth: "120px",
        ...{ style: {} },
    }));
    const __VLS_114 = __VLS_113({
        labelWidth: "120px",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_113));
    __VLS_115.slots.default;
    const __VLS_116 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
        label: "实际现金",
    }));
    const __VLS_118 = __VLS_117({
        label: "实际现金",
    }, ...__VLS_functionalComponentArgsRest(__VLS_117));
    __VLS_119.slots.default;
    const __VLS_120 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
        modelValue: (__VLS_ctx.actualCash),
        precision: (2),
        step: (10),
        min: (0),
        disabled: (__VLS_ctx.preview.alreadyClosed),
        ...{ style: {} },
    }));
    const __VLS_122 = __VLS_121({
        modelValue: (__VLS_ctx.actualCash),
        precision: (2),
        step: (10),
        min: (0),
        disabled: (__VLS_ctx.preview.alreadyClosed),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_121));
    if (!__VLS_ctx.preview.alreadyClosed) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "variance" },
            ...{ class: (__VLS_ctx.varianceClass) },
        });
        (__VLS_ctx.variance.toFixed(2));
    }
    var __VLS_119;
    const __VLS_124 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
        label: "备注",
    }));
    const __VLS_126 = __VLS_125({
        label: "备注",
    }, ...__VLS_functionalComponentArgsRest(__VLS_125));
    __VLS_127.slots.default;
    const __VLS_128 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
        modelValue: (__VLS_ctx.remark),
        type: "textarea",
        rows: (2),
        maxlength: "500",
        disabled: (__VLS_ctx.preview.alreadyClosed),
    }));
    const __VLS_130 = __VLS_129({
        modelValue: (__VLS_ctx.remark),
        type: "textarea",
        rows: (2),
        maxlength: "500",
        disabled: (__VLS_ctx.preview.alreadyClosed),
    }, ...__VLS_functionalComponentArgsRest(__VLS_129));
    var __VLS_127;
    if (!__VLS_ctx.preview.alreadyClosed) {
        const __VLS_132 = {}.ElFormItem;
        /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
        // @ts-ignore
        const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({}));
        const __VLS_134 = __VLS_133({}, ...__VLS_functionalComponentArgsRest(__VLS_133));
        __VLS_135.slots.default;
        const __VLS_136 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
            ...{ 'onClick': {} },
            type: "primary",
            loading: (__VLS_ctx.submitting),
        }));
        const __VLS_138 = __VLS_137({
            ...{ 'onClick': {} },
            type: "primary",
            loading: (__VLS_ctx.submitting),
        }, ...__VLS_functionalComponentArgsRest(__VLS_137));
        let __VLS_140;
        let __VLS_141;
        let __VLS_142;
        const __VLS_143 = {
            onClick: (__VLS_ctx.submit)
        };
        __VLS_139.slots.default;
        var __VLS_139;
        var __VLS_135;
    }
    var __VLS_115;
}
var __VLS_3;
const __VLS_144 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    shadow: "never",
    ...{ style: {} },
}));
const __VLS_146 = __VLS_145({
    shadow: "never",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
__VLS_147.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_147.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
}
const __VLS_148 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    data: (__VLS_ctx.history),
    stripe: true,
}));
const __VLS_150 = __VLS_149({
    data: (__VLS_ctx.history),
    stripe: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
__VLS_151.slots.default;
const __VLS_152 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
    label: "日期",
    width: "120",
}));
const __VLS_154 = __VLS_153({
    label: "日期",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_153));
__VLS_155.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_155.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.dayjs(row.businessDate).format('YYYY-MM-DD'));
}
var __VLS_155;
const __VLS_156 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
    label: "订单数",
    width: "80",
    prop: "orderCount",
}));
const __VLS_158 = __VLS_157({
    label: "订单数",
    width: "80",
    prop: "orderCount",
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
const __VLS_160 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
    label: "营业额",
    width: "120",
}));
const __VLS_162 = __VLS_161({
    label: "营业额",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_161));
__VLS_163.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_163.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.revenueTotal.toFixed(2));
}
var __VLS_163;
const __VLS_164 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    label: "预期现金",
    width: "120",
}));
const __VLS_166 = __VLS_165({
    label: "预期现金",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_167.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_167.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.expectedCash.toFixed(2));
}
var __VLS_167;
const __VLS_168 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
    label: "实收现金",
    width: "120",
}));
const __VLS_170 = __VLS_169({
    label: "实收现金",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_169));
__VLS_171.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_171.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.actualCash.toFixed(2));
}
var __VLS_171;
const __VLS_172 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
    label: "差额",
    width: "120",
}));
const __VLS_174 = __VLS_173({
    label: "差额",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_173));
__VLS_175.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_175.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_176 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
        type: (row.variance === 0 ? 'success' : (row.variance < 0 ? 'danger' : 'warning')),
    }));
    const __VLS_178 = __VLS_177({
        type: (row.variance === 0 ? 'success' : (row.variance < 0 ? 'danger' : 'warning')),
    }, ...__VLS_functionalComponentArgsRest(__VLS_177));
    __VLS_179.slots.default;
    (row.variance.toFixed(2));
    var __VLS_179;
}
var __VLS_175;
const __VLS_180 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
    label: "操作员",
    width: "120",
    prop: "operatorName",
}));
const __VLS_182 = __VLS_181({
    label: "操作员",
    width: "120",
    prop: "operatorName",
}, ...__VLS_functionalComponentArgsRest(__VLS_181));
const __VLS_184 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
    label: "备注",
    minWidth: "160",
    prop: "remark",
    showOverflowTooltip: true,
}));
const __VLS_186 = __VLS_185({
    label: "备注",
    minWidth: "160",
    prop: "remark",
    showOverflowTooltip: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_185));
var __VLS_151;
var __VLS_147;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['preview']} */ ;
/** @type {__VLS_StyleScopedClasses['variance']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Refresh: Refresh,
            dayjs: dayjs,
            businessDate: businessDate,
            preview: preview,
            actualCash: actualCash,
            remark: remark,
            submitting: submitting,
            history: history,
            variance: variance,
            varianceClass: varianceClass,
            disableFuture: disableFuture,
            loadPreview: loadPreview,
            submit: submit,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
