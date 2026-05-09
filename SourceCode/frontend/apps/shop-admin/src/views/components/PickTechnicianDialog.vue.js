import { computed, reactive } from 'vue';
const props = defineProps();
const emit = defineEmits();
const form = reactive({ technicianId: null, quantity: 1 });
const unit = computed(() => {
    if (!props.service)
        return 0;
    return props.isMember ? props.service.memberPrice : props.service.price;
});
function reset() {
    form.technicianId = null;
    form.quantity = 1;
}
function confirm() {
    if (form.technicianId == null)
        return;
    emit('confirm', { technicianId: form.technicianId, quantity: form.quantity });
    reset();
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
    ...{ 'onClose': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: (`指派技师：${__VLS_ctx.service?.name ?? ''}`),
    width: "420px",
}));
const __VLS_2 = __VLS_1({
    ...{ 'onUpdate:modelValue': {} },
    ...{ 'onClose': {} },
    modelValue: (__VLS_ctx.modelValue),
    title: (`指派技师：${__VLS_ctx.service?.name ?? ''}`),
    width: "420px",
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
if (__VLS_ctx.service) {
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
        label: "单价",
    }));
    const __VLS_16 = __VLS_15({
        label: "单价",
    }, ...__VLS_functionalComponentArgsRest(__VLS_15));
    __VLS_17.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "price" },
    });
    (__VLS_ctx.unit.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "muted" },
        ...{ style: {} },
    });
    (__VLS_ctx.service.durationMinutes);
    var __VLS_17;
    const __VLS_18 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_19 = __VLS_asFunctionalComponent(__VLS_18, new __VLS_18({
        label: "技师",
    }));
    const __VLS_20 = __VLS_19({
        label: "技师",
    }, ...__VLS_functionalComponentArgsRest(__VLS_19));
    __VLS_21.slots.default;
    const __VLS_22 = {}.ElSelect;
    /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
    // @ts-ignore
    const __VLS_23 = __VLS_asFunctionalComponent(__VLS_22, new __VLS_22({
        modelValue: (__VLS_ctx.form.technicianId),
        filterable: true,
        placeholder: "搜索工号或姓名",
        ...{ style: {} },
    }));
    const __VLS_24 = __VLS_23({
        modelValue: (__VLS_ctx.form.technicianId),
        filterable: true,
        placeholder: "搜索工号或姓名",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_23));
    __VLS_25.slots.default;
    for (const [t] of __VLS_getVForSourceType((__VLS_ctx.technicians))) {
        const __VLS_26 = {}.ElOption;
        /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
        // @ts-ignore
        const __VLS_27 = __VLS_asFunctionalComponent(__VLS_26, new __VLS_26({
            key: (t.id),
            label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
            value: (t.id),
        }));
        const __VLS_28 = __VLS_27({
            key: (t.id),
            label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
            value: (t.id),
        }, ...__VLS_functionalComponentArgsRest(__VLS_27));
    }
    var __VLS_25;
    var __VLS_21;
    const __VLS_30 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_31 = __VLS_asFunctionalComponent(__VLS_30, new __VLS_30({
        label: "数量",
    }));
    const __VLS_32 = __VLS_31({
        label: "数量",
    }, ...__VLS_functionalComponentArgsRest(__VLS_31));
    __VLS_33.slots.default;
    const __VLS_34 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_35 = __VLS_asFunctionalComponent(__VLS_34, new __VLS_34({
        modelValue: (__VLS_ctx.form.quantity),
        min: (1),
        max: (10),
    }));
    const __VLS_36 = __VLS_35({
        modelValue: (__VLS_ctx.form.quantity),
        min: (1),
        max: (10),
    }, ...__VLS_functionalComponentArgsRest(__VLS_35));
    var __VLS_33;
    var __VLS_13;
}
{
    const { footer: __VLS_thisSlot } = __VLS_3.slots;
    const __VLS_38 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_39 = __VLS_asFunctionalComponent(__VLS_38, new __VLS_38({
        ...{ 'onClick': {} },
    }));
    const __VLS_40 = __VLS_39({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_39));
    let __VLS_42;
    let __VLS_43;
    let __VLS_44;
    const __VLS_45 = {
        onClick: (...[$event]) => {
            __VLS_ctx.emit('update:modelValue', false);
        }
    };
    __VLS_41.slots.default;
    var __VLS_41;
    const __VLS_46 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_47 = __VLS_asFunctionalComponent(__VLS_46, new __VLS_46({
        ...{ 'onClick': {} },
        type: "primary",
        disabled: (!__VLS_ctx.form.technicianId),
    }));
    const __VLS_48 = __VLS_47({
        ...{ 'onClick': {} },
        type: "primary",
        disabled: (!__VLS_ctx.form.technicianId),
    }, ...__VLS_functionalComponentArgsRest(__VLS_47));
    let __VLS_50;
    let __VLS_51;
    let __VLS_52;
    const __VLS_53 = {
        onClick: (__VLS_ctx.confirm)
    };
    __VLS_49.slots.default;
    var __VLS_49;
}
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['price']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            emit: emit,
            form: form,
            unit: unit,
            reset: reset,
            confirm: confirm,
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
