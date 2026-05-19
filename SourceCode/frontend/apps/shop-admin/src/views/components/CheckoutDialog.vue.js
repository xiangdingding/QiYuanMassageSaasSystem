import { computed, reactive, watch } from 'vue';
const props = defineProps();
const emit = defineEmits();
const form = reactive({
    payMethod: 'Cash',
    paidAmount: 0,
    remark: ''
});
watch(() => props.payable, (v) => {
    form.paidAmount = v;
}, { immediate: true });
watch(() => form.payMethod, (m) => {
    if (m !== 'Cash')
        form.paidAmount = props.payable;
});
const canSubmit = computed(() => {
    if (form.payMethod === 'MemberCard' && props.memberBalance < props.payable)
        return false;
    if (form.payMethod === 'Cash' && form.paidAmount < props.payable)
        return false;
    return true;
});
function onOpen() {
    form.payMethod = 'Cash';
    form.paidAmount = props.payable;
    form.remark = '';
    // 弹窗打开后把焦点放到第一个 RadioButton（el-dialog 自带焦点陷阱，Tab 不会跑出去）
    // Element Plus 已自动 focus 到 dialog 内首个可聚焦元素，不再额外处理
}
function onCtrlEnter() {
    if (canSubmit.value)
        submit();
}
function submit() {
    emit('submit', {
        payMethod: form.payMethod,
        paidAmount: form.payMethod === 'Cash' ? form.paidAmount : null,
        remark: form.remark || null
    });
}
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
// CSS variable injection 
// CSS variable injection end 
const __VLS_0 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ 'onUpdate:modelValue': {} },
    ...{ 'onOpen': {} },
    ...{ 'onKeydown': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: "结账",
    width: "460px",
    'aria-label': "结账对话框，按 Esc 关闭，Ctrl+回车确认",
    closeOnPressEscape: (true),
    closeOnClickModal: (false),
}));
const __VLS_2 = __VLS_1({
    ...{ 'onUpdate:modelValue': {} },
    ...{ 'onOpen': {} },
    ...{ 'onKeydown': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: "结账",
    width: "460px",
    'aria-label': "结账对话框，按 Esc 关闭，Ctrl+回车确认",
    closeOnPressEscape: (true),
    closeOnClickModal: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
let __VLS_4;
let __VLS_5;
let __VLS_6;
const __VLS_7 = {
    'onUpdate:modelValue': ((v) => __VLS_ctx.emit('update:modelValue', v))
};
const __VLS_8 = {
    onOpen: (__VLS_ctx.onOpen)
};
const __VLS_9 = {
    onKeydown: (__VLS_ctx.onCtrlEnter)
};
var __VLS_10 = {};
__VLS_3.slots.default;
const __VLS_11 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_12 = __VLS_asFunctionalComponent(__VLS_11, new __VLS_11({
    model: (__VLS_ctx.form),
    labelWidth: "90px",
}));
const __VLS_13 = __VLS_12({
    model: (__VLS_ctx.form),
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_12));
__VLS_14.slots.default;
const __VLS_15 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_16 = __VLS_asFunctionalComponent(__VLS_15, new __VLS_15({
    label: "应收",
}));
const __VLS_17 = __VLS_16({
    label: "应收",
}, ...__VLS_functionalComponentArgsRest(__VLS_16));
__VLS_18.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "payable" },
});
(__VLS_ctx.payable.toFixed(2));
if (__VLS_ctx.total !== __VLS_ctx.payable) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "muted" },
        ...{ style: {} },
    });
    (__VLS_ctx.total.toFixed(2));
}
var __VLS_18;
const __VLS_19 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_20 = __VLS_asFunctionalComponent(__VLS_19, new __VLS_19({
    label: "支付方式",
}));
const __VLS_21 = __VLS_20({
    label: "支付方式",
}, ...__VLS_functionalComponentArgsRest(__VLS_20));
__VLS_22.slots.default;
const __VLS_23 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_24 = __VLS_asFunctionalComponent(__VLS_23, new __VLS_23({
    modelValue: (__VLS_ctx.form.payMethod),
}));
const __VLS_25 = __VLS_24({
    modelValue: (__VLS_ctx.form.payMethod),
}, ...__VLS_functionalComponentArgsRest(__VLS_24));
__VLS_26.slots.default;
const __VLS_27 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_28 = __VLS_asFunctionalComponent(__VLS_27, new __VLS_27({
    value: "Cash",
}));
const __VLS_29 = __VLS_28({
    value: "Cash",
}, ...__VLS_functionalComponentArgsRest(__VLS_28));
__VLS_30.slots.default;
var __VLS_30;
const __VLS_31 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_32 = __VLS_asFunctionalComponent(__VLS_31, new __VLS_31({
    value: "MemberCard",
    disabled: (!__VLS_ctx.hasMember),
}));
const __VLS_33 = __VLS_32({
    value: "MemberCard",
    disabled: (!__VLS_ctx.hasMember),
}, ...__VLS_functionalComponentArgsRest(__VLS_32));
__VLS_34.slots.default;
var __VLS_34;
const __VLS_35 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_36 = __VLS_asFunctionalComponent(__VLS_35, new __VLS_35({
    value: "Wechat",
}));
const __VLS_37 = __VLS_36({
    value: "Wechat",
}, ...__VLS_functionalComponentArgsRest(__VLS_36));
__VLS_38.slots.default;
var __VLS_38;
const __VLS_39 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_40 = __VLS_asFunctionalComponent(__VLS_39, new __VLS_39({
    value: "Alipay",
}));
const __VLS_41 = __VLS_40({
    value: "Alipay",
}, ...__VLS_functionalComponentArgsRest(__VLS_40));
__VLS_42.slots.default;
var __VLS_42;
const __VLS_43 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_44 = __VLS_asFunctionalComponent(__VLS_43, new __VLS_43({
    value: "BankCard",
}));
const __VLS_45 = __VLS_44({
    value: "BankCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_44));
__VLS_46.slots.default;
var __VLS_46;
var __VLS_26;
var __VLS_22;
if (__VLS_ctx.form.payMethod === 'MemberCard') {
    const __VLS_47 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_48 = __VLS_asFunctionalComponent(__VLS_47, new __VLS_47({
        label: "会员余额",
    }));
    const __VLS_49 = __VLS_48({
        label: "会员余额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_48));
    __VLS_50.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: ({ insufficient: __VLS_ctx.memberBalance < __VLS_ctx.payable }) },
    });
    (__VLS_ctx.memberBalance.toFixed(2));
    if (__VLS_ctx.memberBalance < __VLS_ctx.payable) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "muted" },
            ...{ style: {} },
        });
    }
    var __VLS_50;
}
if (__VLS_ctx.form.payMethod === 'Cash') {
    const __VLS_51 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_52 = __VLS_asFunctionalComponent(__VLS_51, new __VLS_51({
        label: "实收金额",
    }));
    const __VLS_53 = __VLS_52({
        label: "实收金额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_52));
    __VLS_54.slots.default;
    const __VLS_55 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_56 = __VLS_asFunctionalComponent(__VLS_55, new __VLS_55({
        modelValue: (__VLS_ctx.form.paidAmount),
        min: (__VLS_ctx.payable),
        precision: (2),
        step: (10),
        ...{ style: {} },
    }));
    const __VLS_57 = __VLS_56({
        modelValue: (__VLS_ctx.form.paidAmount),
        min: (__VLS_ctx.payable),
        precision: (2),
        step: (10),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_56));
    if (__VLS_ctx.form.paidAmount && __VLS_ctx.form.paidAmount > __VLS_ctx.payable) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "muted" },
            ...{ style: {} },
        });
        ((__VLS_ctx.form.paidAmount - __VLS_ctx.payable).toFixed(2));
    }
    var __VLS_54;
}
const __VLS_59 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_60 = __VLS_asFunctionalComponent(__VLS_59, new __VLS_59({
    label: "备注",
}));
const __VLS_61 = __VLS_60({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_60));
__VLS_62.slots.default;
const __VLS_63 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_64 = __VLS_asFunctionalComponent(__VLS_63, new __VLS_63({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    placeholder: "可选",
}));
const __VLS_65 = __VLS_64({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    placeholder: "可选",
}, ...__VLS_functionalComponentArgsRest(__VLS_64));
var __VLS_62;
var __VLS_14;
{
    const { footer: __VLS_thisSlot } = __VLS_3.slots;
    const __VLS_67 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_68 = __VLS_asFunctionalComponent(__VLS_67, new __VLS_67({
        ...{ 'onClick': {} },
    }));
    const __VLS_69 = __VLS_68({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_68));
    let __VLS_71;
    let __VLS_72;
    let __VLS_73;
    const __VLS_74 = {
        onClick: (...[$event]) => {
            __VLS_ctx.emit('update:modelValue', false);
        }
    };
    __VLS_70.slots.default;
    var __VLS_70;
    const __VLS_75 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_76 = __VLS_asFunctionalComponent(__VLS_75, new __VLS_75({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.loading),
        disabled: (!__VLS_ctx.canSubmit),
    }));
    const __VLS_77 = __VLS_76({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.loading),
        disabled: (!__VLS_ctx.canSubmit),
    }, ...__VLS_functionalComponentArgsRest(__VLS_76));
    let __VLS_79;
    let __VLS_80;
    let __VLS_81;
    const __VLS_82 = {
        onClick: (__VLS_ctx.submit)
    };
    __VLS_78.slots.default;
    var __VLS_78;
}
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['payable']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            emit: emit,
            form: form,
            canSubmit: canSubmit,
            onOpen: onOpen,
            onCtrlEnter: onCtrlEnter,
            submit: submit,
        };
    },
    __typeEmits: {},
    __typeProps: {},
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
    __typeEmits: {},
    __typeProps: {},
});
; /* PartiallyEnd: #4569/main.vue */
