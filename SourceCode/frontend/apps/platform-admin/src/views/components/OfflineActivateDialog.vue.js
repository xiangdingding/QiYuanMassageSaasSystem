import { reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { subscriptionsApi } from '@/api/modules';
const props = defineProps();
const emit = defineEmits();
const formRef = ref();
const loading = ref(false);
const form = reactive({
    planId: null,
    years: 1,
    amountReceived: 0,
    remark: ''
});
const rules = {
    planId: [{ required: true, message: '请选择套餐', trigger: 'change' }],
    years: [{ required: true, message: '请填写年限', trigger: 'blur' }],
    amountReceived: [{ required: true, message: '请填写实收金额', trigger: 'blur' }]
};
watch(() => [form.planId, form.years], ([pid, years]) => {
    if (pid != null && form.amountReceived === 0) {
        const plan = props.plans.find((p) => p.id === pid);
        if (plan)
            form.amountReceived = +(plan.annualPrice * years).toFixed(2);
    }
});
watch(() => props.modelValue, (v) => {
    if (v && props.tenant?.currentPlanId) {
        form.planId = props.tenant.currentPlanId;
    }
});
function reset() {
    form.planId = null;
    form.years = 1;
    form.amountReceived = 0;
    form.remark = '';
}
async function submit() {
    if (!formRef.value || !props.tenant)
        return;
    const ok = await formRef.value.validate().catch(() => false);
    if (!ok)
        return;
    loading.value = true;
    try {
        await subscriptionsApi.activateOffline({
            tenantId: props.tenant.id,
            planId: form.planId,
            years: form.years,
            amountReceived: form.amountReceived,
            remark: form.remark || null
        });
        ElMessage.success('激活成功');
        emit('activated');
        emit('update:modelValue', false);
    }
    finally {
        loading.value = false;
    }
}
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
const __VLS_0 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ 'onUpdate:modelValue': {} },
    ...{ 'onClose': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: (`线下续费/激活：${__VLS_ctx.tenant?.name ?? ''}`),
    width: "480px",
}));
const __VLS_2 = __VLS_1({
    ...{ 'onUpdate:modelValue': {} },
    ...{ 'onClose': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: (`线下续费/激活：${__VLS_ctx.tenant?.name ?? ''}`),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
let __VLS_4;
let __VLS_5;
let __VLS_6;
const __VLS_7 = {
    'onUpdate:modelValue': ((v) => __VLS_ctx.emit('update:modelValue', v))
};
const __VLS_8 = {
    onClose: (__VLS_ctx.reset)
};
var __VLS_9 = {};
__VLS_3.slots.default;
const __VLS_10 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_11 = __VLS_asFunctionalComponent(__VLS_10, new __VLS_10({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "120px",
}));
const __VLS_12 = __VLS_11({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "120px",
}, ...__VLS_functionalComponentArgsRest(__VLS_11));
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_14 = {};
__VLS_13.slots.default;
const __VLS_16 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    label: "套餐",
    prop: "planId",
}));
const __VLS_18 = __VLS_17({
    label: "套餐",
    prop: "planId",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
__VLS_19.slots.default;
const __VLS_20 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    modelValue: (__VLS_ctx.form.planId),
    placeholder: "请选择",
    ...{ style: {} },
}));
const __VLS_22 = __VLS_21({
    modelValue: (__VLS_ctx.form.planId),
    placeholder: "请选择",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
__VLS_23.slots.default;
for (const [p] of __VLS_getVForSourceType((__VLS_ctx.plans))) {
    const __VLS_24 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
        key: (p.id),
        label: (`${p.name} - ¥${p.annualPrice}/年`),
        value: (p.id),
    }));
    const __VLS_26 = __VLS_25({
        key: (p.id),
        label: (`${p.name} - ¥${p.annualPrice}/年`),
        value: (p.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_25));
}
var __VLS_23;
var __VLS_19;
const __VLS_28 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    label: "购买年限",
    prop: "years",
}));
const __VLS_30 = __VLS_29({
    label: "购买年限",
    prop: "years",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
__VLS_31.slots.default;
const __VLS_32 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    modelValue: (__VLS_ctx.form.years),
    min: (1),
    max: (10),
}));
const __VLS_34 = __VLS_33({
    modelValue: (__VLS_ctx.form.years),
    min: (1),
    max: (10),
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
var __VLS_31;
const __VLS_36 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    label: "实收金额",
    prop: "amountReceived",
}));
const __VLS_38 = __VLS_37({
    label: "实收金额",
    prop: "amountReceived",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
__VLS_39.slots.default;
const __VLS_40 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    modelValue: (__VLS_ctx.form.amountReceived),
    min: (0),
    precision: (2),
    step: (100),
    ...{ style: {} },
}));
const __VLS_42 = __VLS_41({
    modelValue: (__VLS_ctx.form.amountReceived),
    min: (0),
    precision: (2),
    step: (100),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ style: {} },
});
var __VLS_39;
const __VLS_44 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    label: "备注",
}));
const __VLS_46 = __VLS_45({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
const __VLS_48 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    placeholder: "如：已收到银行转账 12800 元",
}));
const __VLS_50 = __VLS_49({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    placeholder: "如：已收到银行转账 12800 元",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
var __VLS_47;
var __VLS_13;
{
    const { footer: __VLS_thisSlot } = __VLS_3.slots;
    const __VLS_52 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
        ...{ 'onClick': {} },
    }));
    const __VLS_54 = __VLS_53({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_53));
    let __VLS_56;
    let __VLS_57;
    let __VLS_58;
    const __VLS_59 = {
        onClick: (...[$event]) => {
            __VLS_ctx.emit('update:modelValue', false);
        }
    };
    __VLS_55.slots.default;
    var __VLS_55;
    const __VLS_60 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.loading),
    }));
    const __VLS_62 = __VLS_61({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.loading),
    }, ...__VLS_functionalComponentArgsRest(__VLS_61));
    let __VLS_64;
    let __VLS_65;
    let __VLS_66;
    const __VLS_67 = {
        onClick: (__VLS_ctx.submit)
    };
    __VLS_63.slots.default;
    var __VLS_63;
}
var __VLS_3;
// @ts-ignore
var __VLS_15 = __VLS_14;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            emit: emit,
            formRef: formRef,
            loading: loading,
            form: form,
            rules: rules,
            reset: reset,
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
