import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { vouchersApi } from '@/api/modules';
const rows = ref([]);
const loading = ref(false);
const statusFilter = ref(undefined);
const keyword = ref('');
const formOpen = ref(false);
const saving = ref(false);
const form = reactive({
    kind: 'StoreCoupon', code: '', title: '',
    faceValue: 0, minOrderAmount: 0, discountPercent: 0,
    validFrom: null, expiresAt: null,
    platform: '', remark: ''
});
function statusLabel(s) {
    return { Active: '生效中', Redeemed: '已核销', Expired: '已过期', Cancelled: '已作废' }[s] ?? s;
}
function statusTag(s) {
    return s === 'Active' ? 'success' : s === 'Redeemed' ? 'info' : s === 'Expired' ? 'warning' : 'danger';
}
async function reload() {
    loading.value = true;
    try {
        rows.value = await vouchersApi.list({ status: statusFilter.value, keyword: keyword.value || undefined });
    }
    finally {
        loading.value = false;
    }
}
function openNew() {
    Object.assign(form, {
        kind: 'StoreCoupon', code: '', title: '',
        faceValue: 0, minOrderAmount: 0, discountPercent: 0,
        validFrom: null, expiresAt: null, platform: '', remark: ''
    });
    formOpen.value = true;
}
async function save() {
    if (!form.code.trim() || !form.title.trim()) {
        ElMessage.warning('券码与标题必填');
        return;
    }
    saving.value = true;
    try {
        await vouchersApi.create({
            kind: form.kind, code: form.code.trim(), title: form.title.trim(),
            faceValue: form.faceValue, minOrderAmount: form.minOrderAmount,
            discountPercent: form.discountPercent || null,
            validFrom: form.validFrom, expiresAt: form.expiresAt,
            platform: form.platform || null, remark: form.remark || null
        });
        formOpen.value = false;
        ElMessage.success('已创建');
        await reload();
    }
    finally {
        saving.value = false;
    }
}
async function cancel(row) {
    await ElMessageBox.confirm(`确认作废券 ${row.code}？`, '提示', { type: 'warning' }).catch(() => null);
    await vouchersApi.cancel(row.id);
    ElMessage.success('已作废');
    await reload();
}
onMounted(reload);
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
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
const __VLS_4 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.statusFilter),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.statusFilter),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onChange: (__VLS_ctx.reload)
};
__VLS_7.slots.default;
const __VLS_12 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    label: "生效中",
    value: "Active",
}));
const __VLS_14 = __VLS_13({
    label: "生效中",
    value: "Active",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
const __VLS_16 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    label: "已核销",
    value: "Redeemed",
}));
const __VLS_18 = __VLS_17({
    label: "已核销",
    value: "Redeemed",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
const __VLS_20 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    label: "已过期",
    value: "Expired",
}));
const __VLS_22 = __VLS_21({
    label: "已过期",
    value: "Expired",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
const __VLS_24 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    label: "已作废",
    value: "Cancelled",
}));
const __VLS_26 = __VLS_25({
    label: "已作废",
    value: "Cancelled",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
var __VLS_7;
const __VLS_28 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onKeydown': {} },
    modelValue: (__VLS_ctx.keyword),
    placeholder: "搜索券码/标题",
    ...{ style: {} },
    clearable: true,
}));
const __VLS_30 = __VLS_29({
    ...{ 'onKeydown': {} },
    modelValue: (__VLS_ctx.keyword),
    placeholder: "搜索券码/标题",
    ...{ style: {} },
    clearable: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onKeydown: (__VLS_ctx.reload)
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
    type: "primary",
    icon: (__VLS_ctx.Plus),
}));
const __VLS_38 = __VLS_37({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
let __VLS_40;
let __VLS_41;
let __VLS_42;
const __VLS_43 = {
    onClick: (__VLS_ctx.openNew)
};
__VLS_39.slots.default;
var __VLS_39;
const __VLS_44 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_46 = __VLS_45({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
let __VLS_48;
let __VLS_49;
let __VLS_50;
const __VLS_51 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_47.slots.default;
var __VLS_47;
const __VLS_52 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_54 = __VLS_53({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_55.slots.default;
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "kind",
    label: "类型",
    width: "100",
}));
const __VLS_58 = __VLS_57({
    prop: "kind",
    label: "类型",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
__VLS_59.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_59.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.kind === 'GroupBuy' ? '团购券' : '店内券');
}
var __VLS_59;
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    prop: "code",
    label: "券码",
    width: "160",
}));
const __VLS_62 = __VLS_61({
    prop: "code",
    label: "券码",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    prop: "title",
    label: "标题",
    minWidth: "160",
}));
const __VLS_66 = __VLS_65({
    prop: "title",
    label: "标题",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    prop: "faceValue",
    label: "面值",
    width: "100",
}));
const __VLS_70 = __VLS_69({
    prop: "faceValue",
    label: "面值",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    prop: "minOrderAmount",
    label: "最低额",
    width: "100",
}));
const __VLS_74 = __VLS_73({
    prop: "minOrderAmount",
    label: "最低额",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    prop: "discountPercent",
    label: "折扣",
    width: "100",
}));
const __VLS_78 = __VLS_77({
    prop: "discountPercent",
    label: "折扣",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
__VLS_79.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_79.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.discountPercent ? (row.discountPercent * 10).toFixed(1) + '折' : '—');
}
var __VLS_79;
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    prop: "platform",
    label: "平台",
    width: "100",
}));
const __VLS_82 = __VLS_81({
    prop: "platform",
    label: "平台",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    prop: "status",
    label: "状态",
    width: "100",
}));
const __VLS_86 = __VLS_85({
    prop: "status",
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_88 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
        type: (__VLS_ctx.statusTag(row.status)),
    }));
    const __VLS_90 = __VLS_89({
        type: (__VLS_ctx.statusTag(row.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_89));
    __VLS_91.slots.default;
    (__VLS_ctx.statusLabel(row.status));
    var __VLS_91;
}
var __VLS_87;
const __VLS_92 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    prop: "expiresAt",
    label: "到期",
    width: "160",
}));
const __VLS_94 = __VLS_93({
    prop: "expiresAt",
    label: "到期",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
const __VLS_96 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    label: "操作",
    width: "120",
    fixed: "right",
}));
const __VLS_98 = __VLS_97({
    label: "操作",
    width: "120",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
__VLS_99.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_99.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_100 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Active'),
    }));
    const __VLS_102 = __VLS_101({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Active'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
    let __VLS_104;
    let __VLS_105;
    let __VLS_106;
    const __VLS_107 = {
        onClick: (...[$event]) => {
            __VLS_ctx.cancel(row);
        }
    };
    __VLS_103.slots.default;
    var __VLS_103;
}
var __VLS_99;
var __VLS_55;
var __VLS_3;
const __VLS_108 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
    modelValue: (__VLS_ctx.formOpen),
    title: "新建券",
    width: "480px",
}));
const __VLS_110 = __VLS_109({
    modelValue: (__VLS_ctx.formOpen),
    title: "新建券",
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_109));
__VLS_111.slots.default;
const __VLS_112 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    model: (__VLS_ctx.form),
    labelWidth: "100px",
}));
const __VLS_114 = __VLS_113({
    model: (__VLS_ctx.form),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
__VLS_115.slots.default;
const __VLS_116 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
    label: "类型",
    required: true,
}));
const __VLS_118 = __VLS_117({
    label: "类型",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_117));
__VLS_119.slots.default;
const __VLS_120 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    modelValue: (__VLS_ctx.form.kind),
}));
const __VLS_122 = __VLS_121({
    modelValue: (__VLS_ctx.form.kind),
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
const __VLS_124 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    value: "StoreCoupon",
}));
const __VLS_126 = __VLS_125({
    value: "StoreCoupon",
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
__VLS_127.slots.default;
var __VLS_127;
const __VLS_128 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    value: "GroupBuy",
}));
const __VLS_130 = __VLS_129({
    value: "GroupBuy",
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_131.slots.default;
var __VLS_131;
var __VLS_123;
var __VLS_119;
const __VLS_132 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    label: "券码",
    required: true,
}));
const __VLS_134 = __VLS_133({
    label: "券码",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
__VLS_135.slots.default;
const __VLS_136 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
    modelValue: (__VLS_ctx.form.code),
    maxlength: "64",
}));
const __VLS_138 = __VLS_137({
    modelValue: (__VLS_ctx.form.code),
    maxlength: "64",
}, ...__VLS_functionalComponentArgsRest(__VLS_137));
var __VLS_135;
const __VLS_140 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    label: "标题",
    required: true,
}));
const __VLS_142 = __VLS_141({
    label: "标题",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
__VLS_143.slots.default;
const __VLS_144 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    modelValue: (__VLS_ctx.form.title),
    maxlength: "200",
}));
const __VLS_146 = __VLS_145({
    modelValue: (__VLS_ctx.form.title),
    maxlength: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
var __VLS_143;
const __VLS_148 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    label: "面值",
}));
const __VLS_150 = __VLS_149({
    label: "面值",
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
__VLS_151.slots.default;
const __VLS_152 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
    modelValue: (__VLS_ctx.form.faceValue),
    min: (0),
    precision: (2),
}));
const __VLS_154 = __VLS_153({
    modelValue: (__VLS_ctx.form.faceValue),
    min: (0),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_153));
var __VLS_151;
const __VLS_156 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
    label: "最低订单额",
}));
const __VLS_158 = __VLS_157({
    label: "最低订单额",
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
__VLS_159.slots.default;
const __VLS_160 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
    modelValue: (__VLS_ctx.form.minOrderAmount),
    min: (0),
    precision: (2),
}));
const __VLS_162 = __VLS_161({
    modelValue: (__VLS_ctx.form.minOrderAmount),
    min: (0),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_161));
var __VLS_159;
const __VLS_164 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    label: "折扣百分比",
}));
const __VLS_166 = __VLS_165({
    label: "折扣百分比",
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_167.slots.default;
const __VLS_168 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
    modelValue: (__VLS_ctx.form.discountPercent),
    min: (0),
    max: (1),
    step: (0.05),
    precision: (2),
}));
const __VLS_170 = __VLS_169({
    modelValue: (__VLS_ctx.form.discountPercent),
    min: (0),
    max: (1),
    step: (0.05),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_169));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ style: {} },
});
var __VLS_167;
if (__VLS_ctx.form.kind === 'GroupBuy') {
    const __VLS_172 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
        label: "平台",
    }));
    const __VLS_174 = __VLS_173({
        label: "平台",
    }, ...__VLS_functionalComponentArgsRest(__VLS_173));
    __VLS_175.slots.default;
    const __VLS_176 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
        modelValue: (__VLS_ctx.form.platform),
        maxlength: "64",
        placeholder: "Meituan / Dianping",
    }));
    const __VLS_178 = __VLS_177({
        modelValue: (__VLS_ctx.form.platform),
        maxlength: "64",
        placeholder: "Meituan / Dianping",
    }, ...__VLS_functionalComponentArgsRest(__VLS_177));
    var __VLS_175;
}
const __VLS_180 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
    label: "生效起",
}));
const __VLS_182 = __VLS_181({
    label: "生效起",
}, ...__VLS_functionalComponentArgsRest(__VLS_181));
__VLS_183.slots.default;
const __VLS_184 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
    modelValue: (__VLS_ctx.form.validFrom),
    type: "datetime",
}));
const __VLS_186 = __VLS_185({
    modelValue: (__VLS_ctx.form.validFrom),
    type: "datetime",
}, ...__VLS_functionalComponentArgsRest(__VLS_185));
var __VLS_183;
const __VLS_188 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_189 = __VLS_asFunctionalComponent(__VLS_188, new __VLS_188({
    label: "到期",
}));
const __VLS_190 = __VLS_189({
    label: "到期",
}, ...__VLS_functionalComponentArgsRest(__VLS_189));
__VLS_191.slots.default;
const __VLS_192 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_193 = __VLS_asFunctionalComponent(__VLS_192, new __VLS_192({
    modelValue: (__VLS_ctx.form.expiresAt),
    type: "datetime",
}));
const __VLS_194 = __VLS_193({
    modelValue: (__VLS_ctx.form.expiresAt),
    type: "datetime",
}, ...__VLS_functionalComponentArgsRest(__VLS_193));
var __VLS_191;
const __VLS_196 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_197 = __VLS_asFunctionalComponent(__VLS_196, new __VLS_196({
    label: "备注",
}));
const __VLS_198 = __VLS_197({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_197));
__VLS_199.slots.default;
const __VLS_200 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_201 = __VLS_asFunctionalComponent(__VLS_200, new __VLS_200({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_202 = __VLS_201({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_201));
var __VLS_199;
var __VLS_115;
{
    const { footer: __VLS_thisSlot } = __VLS_111.slots;
    const __VLS_204 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_205 = __VLS_asFunctionalComponent(__VLS_204, new __VLS_204({
        ...{ 'onClick': {} },
    }));
    const __VLS_206 = __VLS_205({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_205));
    let __VLS_208;
    let __VLS_209;
    let __VLS_210;
    const __VLS_211 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_207.slots.default;
    var __VLS_207;
    const __VLS_212 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_213 = __VLS_asFunctionalComponent(__VLS_212, new __VLS_212({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_214 = __VLS_213({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_213));
    let __VLS_216;
    let __VLS_217;
    let __VLS_218;
    const __VLS_219 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_215.slots.default;
    var __VLS_215;
}
var __VLS_111;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Plus: Plus,
            Refresh: Refresh,
            rows: rows,
            loading: loading,
            statusFilter: statusFilter,
            keyword: keyword,
            formOpen: formOpen,
            saving: saving,
            form: form,
            statusLabel: statusLabel,
            statusTag: statusTag,
            reload: reload,
            openNew: openNew,
            save: save,
            cancel: cancel,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
