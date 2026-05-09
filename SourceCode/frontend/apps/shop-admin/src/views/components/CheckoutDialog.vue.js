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
    modelValue: (__VLS_ctx.modelValue),
    title: "结账",
    width: "460px",
}));
const __VLS_2 = __VLS_1({
    ...{ 'onUpdate:modelValue': {} },
    ...{ 'onOpen': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: "结账",
    width: "460px",
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
var __VLS_9 = {};
__VLS_3.slots.default;
const __VLS_10 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_11 = __VLS_asFunctionalComponent(__VLS_10, new __VLS_10({
    model: (__VLS_ctx.form),
    labelWidth: "90px",
}));
const __VLS_12 = __VLS_11({
    model: (__VLS_ctx.form),
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_11));
__VLS_13.slots.default;
const __VLS_14 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_15 = __VLS_asFunctionalComponent(__VLS_14, new __VLS_14({
    label: "应收",
}));
const __VLS_16 = __VLS_15({
    label: "应收",
}, ...__VLS_functionalComponentArgsRest(__VLS_15));
__VLS_17.slots.default;
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
var __VLS_17;
const __VLS_18 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_19 = __VLS_asFunctionalComponent(__VLS_18, new __VLS_18({
    label: "支付方式",
}));
const __VLS_20 = __VLS_19({
    label: "支付方式",
}, ...__VLS_functionalComponentArgsRest(__VLS_19));
__VLS_21.slots.default;
const __VLS_22 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_23 = __VLS_asFunctionalComponent(__VLS_22, new __VLS_22({
    modelValue: (__VLS_ctx.form.payMethod),
}));
const __VLS_24 = __VLS_23({
    modelValue: (__VLS_ctx.form.payMethod),
}, ...__VLS_functionalComponentArgsRest(__VLS_23));
__VLS_25.slots.default;
const __VLS_26 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_27 = __VLS_asFunctionalComponent(__VLS_26, new __VLS_26({
    value: "Cash",
}));
const __VLS_28 = __VLS_27({
    value: "Cash",
}, ...__VLS_functionalComponentArgsRest(__VLS_27));
__VLS_29.slots.default;
var __VLS_29;
const __VLS_30 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_31 = __VLS_asFunctionalComponent(__VLS_30, new __VLS_30({
    value: "MemberCard",
    disabled: (!__VLS_ctx.hasMember),
}));
const __VLS_32 = __VLS_31({
    value: "MemberCard",
    disabled: (!__VLS_ctx.hasMember),
}, ...__VLS_functionalComponentArgsRest(__VLS_31));
__VLS_33.slots.default;
var __VLS_33;
const __VLS_34 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_35 = __VLS_asFunctionalComponent(__VLS_34, new __VLS_34({
    value: "Wechat",
}));
const __VLS_36 = __VLS_35({
    value: "Wechat",
}, ...__VLS_functionalComponentArgsRest(__VLS_35));
__VLS_37.slots.default;
var __VLS_37;
const __VLS_38 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_39 = __VLS_asFunctionalComponent(__VLS_38, new __VLS_38({
    value: "Alipay",
}));
const __VLS_40 = __VLS_39({
    value: "Alipay",
}, ...__VLS_functionalComponentArgsRest(__VLS_39));
__VLS_41.slots.default;
var __VLS_41;
const __VLS_42 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_43 = __VLS_asFunctionalComponent(__VLS_42, new __VLS_42({
    value: "BankCard",
}));
const __VLS_44 = __VLS_43({
    value: "BankCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_43));
__VLS_45.slots.default;
var __VLS_45;
var __VLS_25;
var __VLS_21;
if (__VLS_ctx.form.payMethod === 'MemberCard') {
    const __VLS_46 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_47 = __VLS_asFunctionalComponent(__VLS_46, new __VLS_46({
        label: "会员余额",
    }));
    const __VLS_48 = __VLS_47({
        label: "会员余额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_47));
    __VLS_49.slots.default;
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
    var __VLS_49;
}
if (__VLS_ctx.form.payMethod === 'Cash') {
    const __VLS_50 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_51 = __VLS_asFunctionalComponent(__VLS_50, new __VLS_50({
        label: "实收金额",
    }));
    const __VLS_52 = __VLS_51({
        label: "实收金额",
    }, ...__VLS_functionalComponentArgsRest(__VLS_51));
    __VLS_53.slots.default;
    const __VLS_54 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_55 = __VLS_asFunctionalComponent(__VLS_54, new __VLS_54({
        modelValue: (__VLS_ctx.form.paidAmount),
        min: (__VLS_ctx.payable),
        precision: (2),
        step: (10),
        ...{ style: {} },
    }));
    const __VLS_56 = __VLS_55({
        modelValue: (__VLS_ctx.form.paidAmount),
        min: (__VLS_ctx.payable),
        precision: (2),
        step: (10),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_55));
    if (__VLS_ctx.form.paidAmount && __VLS_ctx.form.paidAmount > __VLS_ctx.payable) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "muted" },
            ...{ style: {} },
        });
        ((__VLS_ctx.form.paidAmount - __VLS_ctx.payable).toFixed(2));
    }
    var __VLS_53;
}
const __VLS_58 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_59 = __VLS_asFunctionalComponent(__VLS_58, new __VLS_58({
    label: "备注",
}));
const __VLS_60 = __VLS_59({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_59));
__VLS_61.slots.default;
const __VLS_62 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_63 = __VLS_asFunctionalComponent(__VLS_62, new __VLS_62({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    placeholder: "可选",
}));
const __VLS_64 = __VLS_63({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    placeholder: "可选",
}, ...__VLS_functionalComponentArgsRest(__VLS_63));
var __VLS_61;
var __VLS_13;
{
    const { footer: __VLS_thisSlot } = __VLS_3.slots;
    const __VLS_66 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_67 = __VLS_asFunctionalComponent(__VLS_66, new __VLS_66({
        ...{ 'onClick': {} },
    }));
    const __VLS_68 = __VLS_67({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_67));
    let __VLS_70;
    let __VLS_71;
    let __VLS_72;
    const __VLS_73 = {
        onClick: (...[$event]) => {
            __VLS_ctx.emit('update:modelValue', false);
        }
    };
    __VLS_69.slots.default;
    var __VLS_69;
    const __VLS_74 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_75 = __VLS_asFunctionalComponent(__VLS_74, new __VLS_74({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.loading),
        disabled: (!__VLS_ctx.canSubmit),
    }));
    const __VLS_76 = __VLS_75({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.loading),
        disabled: (!__VLS_ctx.canSubmit),
    }, ...__VLS_functionalComponentArgsRest(__VLS_75));
    let __VLS_78;
    let __VLS_79;
    let __VLS_80;
    const __VLS_81 = {
        onClick: (__VLS_ctx.submit)
    };
    __VLS_77.slots.default;
    var __VLS_77;
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
