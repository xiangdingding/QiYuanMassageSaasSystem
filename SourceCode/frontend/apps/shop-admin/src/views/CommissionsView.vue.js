import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { commissionsApi, servicesApi, staffApi } from '@/api/modules';
const rows = ref([]);
const services = ref([]);
const technicians = ref([]);
const loading = ref(false);
const saving = ref(false);
const filterServiceId = ref(null);
const filterTechnicianId = ref(null);
const formOpen = ref(false);
const formMode = ref('create');
const editingId = ref(null);
const form = reactive({
    serviceId: null,
    technicianId: null,
    ruleType: 'FixedAmount',
    amount: 0,
    tieredRulesJson: '',
    priority: 0,
    isActive: true
});
function ruleLabel(t) {
    return { FixedAmount: '固定金额', Percentage: '百分比', Tiered: '阶梯', Timed: '按时计费' }[t] ?? t;
}
function formatAmount(row) {
    if (row.ruleType === 'Percentage')
        return `${row.amount}%`;
    if (row.ruleType === 'Timed')
        return `¥${row.amount}/小时`;
    return `¥${row.amount.toFixed(2)}`;
}
const amountLabel = computed(() => {
    switch (form.ruleType) {
        case 'Percentage': return '百分比';
        case 'Timed': return '元/小时';
        case 'Tiered': return '默认数值';
        default: return '提成金额';
    }
});
const amountHint = computed(() => {
    switch (form.ruleType) {
        case 'Percentage': return '0~100，按订单项金额比例';
        case 'Timed': return '按服务时长比例计算';
        case 'Tiered': return '阶梯未匹配时使用';
        default: return '元';
    }
});
async function reload() {
    loading.value = true;
    try {
        rows.value = await commissionsApi.list(filterServiceId.value ?? undefined, filterTechnicianId.value ?? undefined);
    }
    finally {
        loading.value = false;
    }
}
async function loadOptions() {
    const [s, t] = await Promise.all([
        servicesApi.list(true),
        staffApi.list({ role: 'Technician', pageSize: 200 })
    ]);
    services.value = s;
    technicians.value = t.items;
}
function openCreate() {
    formMode.value = 'create';
    editingId.value = null;
    Object.assign(form, { serviceId: null, technicianId: null, ruleType: 'FixedAmount', amount: 0, tieredRulesJson: '', priority: 0, isActive: true });
    formOpen.value = true;
}
function openEdit(row) {
    formMode.value = 'edit';
    editingId.value = row.id;
    Object.assign(form, {
        serviceId: row.serviceId,
        technicianId: row.technicianId,
        ruleType: row.ruleType,
        amount: row.amount,
        tieredRulesJson: row.tieredRulesJson ?? '',
        priority: row.priority,
        isActive: row.isActive
    });
    formOpen.value = true;
}
async function save() {
    saving.value = true;
    try {
        const body = {
            serviceId: form.serviceId,
            technicianId: form.technicianId,
            ruleType: form.ruleType,
            amount: form.amount,
            tieredRulesJson: form.tieredRulesJson || null,
            priority: form.priority,
            isActive: form.isActive
        };
        if (formMode.value === 'create') {
            await commissionsApi.create(body);
        }
        else if (editingId.value != null) {
            const { serviceId: _s, technicianId: _t, ...rest } = body;
            await commissionsApi.update(editingId.value, rest);
        }
        ElMessage.success('已保存');
        formOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
async function onDelete(row) {
    await ElMessageBox.confirm('确认删除该规则？', '提示', { type: 'warning' }).catch(() => null);
    await commissionsApi.remove(row.id);
    ElMessage.success('已删除');
    reload();
}
onMounted(async () => {
    await loadOptions();
    reload();
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
    modelValue: (__VLS_ctx.filterServiceId),
    placeholder: "按服务过滤",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    modelValue: (__VLS_ctx.filterServiceId),
    placeholder: "按服务过滤",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
__VLS_7.slots.default;
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.services))) {
    const __VLS_8 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
        key: (s.id),
        label: (`${s.code} ${s.name}`),
        value: (s.id),
    }));
    const __VLS_10 = __VLS_9({
        key: (s.id),
        label: (`${s.code} ${s.name}`),
        value: (s.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_9));
}
var __VLS_7;
const __VLS_12 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    modelValue: (__VLS_ctx.filterTechnicianId),
    placeholder: "按技师过滤",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}));
const __VLS_14 = __VLS_13({
    modelValue: (__VLS_ctx.filterTechnicianId),
    placeholder: "按技师过滤",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
__VLS_15.slots.default;
for (const [t] of __VLS_getVForSourceType((__VLS_ctx.technicians))) {
    const __VLS_16 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
        key: (t.id),
        label: (`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`),
        value: (t.id),
    }));
    const __VLS_18 = __VLS_17({
        key: (t.id),
        label: (`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`),
        value: (t.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_17));
}
var __VLS_15;
const __VLS_20 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_22 = __VLS_21({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
let __VLS_24;
let __VLS_25;
let __VLS_26;
const __VLS_27 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_23.slots.default;
var __VLS_23;
const __VLS_28 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onClick': {} },
    type: "success",
}));
const __VLS_30 = __VLS_29({
    ...{ 'onClick': {} },
    type: "success",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onClick: (__VLS_ctx.openCreate)
};
__VLS_31.slots.default;
var __VLS_31;
const __VLS_36 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_38 = __VLS_37({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_39.slots.default;
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    label: "规则类型",
    width: "100",
}));
const __VLS_42 = __VLS_41({
    label: "规则类型",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_43.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_43.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.ruleLabel(row.ruleType));
}
var __VLS_43;
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    label: "适用服务",
    minWidth: "160",
}));
const __VLS_46 = __VLS_45({
    label: "适用服务",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_47.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.serviceName ?? '全部服务');
}
var __VLS_47;
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    label: "适用技师",
    minWidth: "160",
}));
const __VLS_50 = __VLS_49({
    label: "适用技师",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
__VLS_51.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_51.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.technicianName ?? '全部技师');
}
var __VLS_51;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    label: "数值",
    width: "100",
}));
const __VLS_54 = __VLS_53({
    label: "数值",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_55.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_55.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.formatAmount(row));
}
var __VLS_55;
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "priority",
    label: "优先级",
    width: "80",
}));
const __VLS_58 = __VLS_57({
    prop: "priority",
    label: "优先级",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "状态",
    width: "80",
}));
const __VLS_62 = __VLS_61({
    label: "状态",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_64 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        type: (row.isActive ? 'success' : 'info'),
    }));
    const __VLS_66 = __VLS_65({
        type: (row.isActive ? 'success' : 'info'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    __VLS_67.slots.default;
    (row.isActive ? '启用' : '停用');
    var __VLS_67;
}
var __VLS_63;
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    label: "操作",
    width: "160",
}));
const __VLS_70 = __VLS_69({
    label: "操作",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
__VLS_71.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_71.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_72 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_74 = __VLS_73({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_73));
    let __VLS_76;
    let __VLS_77;
    let __VLS_78;
    const __VLS_79 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openEdit(row);
        }
    };
    __VLS_75.slots.default;
    var __VLS_75;
    const __VLS_80 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
    }));
    const __VLS_82 = __VLS_81({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
    }, ...__VLS_functionalComponentArgsRest(__VLS_81));
    let __VLS_84;
    let __VLS_85;
    let __VLS_86;
    const __VLS_87 = {
        onClick: (...[$event]) => {
            __VLS_ctx.onDelete(row);
        }
    };
    __VLS_83.slots.default;
    var __VLS_83;
}
var __VLS_71;
var __VLS_39;
var __VLS_3;
const __VLS_88 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '新建提成规则' : '编辑提成规则'),
    width: "500px",
}));
const __VLS_90 = __VLS_89({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '新建提成规则' : '编辑提成规则'),
    width: "500px",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
__VLS_91.slots.default;
const __VLS_92 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    model: (__VLS_ctx.form),
    labelWidth: "120px",
}));
const __VLS_94 = __VLS_93({
    model: (__VLS_ctx.form),
    labelWidth: "120px",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
__VLS_95.slots.default;
const __VLS_96 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    label: "适用服务",
}));
const __VLS_98 = __VLS_97({
    label: "适用服务",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
__VLS_99.slots.default;
const __VLS_100 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    modelValue: (__VLS_ctx.form.serviceId),
    placeholder: "留空 = 全部服务",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}));
const __VLS_102 = __VLS_101({
    modelValue: (__VLS_ctx.form.serviceId),
    placeholder: "留空 = 全部服务",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_103.slots.default;
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.services))) {
    const __VLS_104 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
        key: (s.id),
        label: (`${s.code} ${s.name}`),
        value: (s.id),
    }));
    const __VLS_106 = __VLS_105({
        key: (s.id),
        label: (`${s.code} ${s.name}`),
        value: (s.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_105));
}
var __VLS_103;
var __VLS_99;
const __VLS_108 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
    label: "适用技师",
}));
const __VLS_110 = __VLS_109({
    label: "适用技师",
}, ...__VLS_functionalComponentArgsRest(__VLS_109));
__VLS_111.slots.default;
const __VLS_112 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    modelValue: (__VLS_ctx.form.technicianId),
    placeholder: "留空 = 全部技师",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}));
const __VLS_114 = __VLS_113({
    modelValue: (__VLS_ctx.form.technicianId),
    placeholder: "留空 = 全部技师",
    clearable: true,
    filterable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
__VLS_115.slots.default;
for (const [t] of __VLS_getVForSourceType((__VLS_ctx.technicians))) {
    const __VLS_116 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
        key: (t.id),
        label: (`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`),
        value: (t.id),
    }));
    const __VLS_118 = __VLS_117({
        key: (t.id),
        label: (`${t.employeeNo ?? ''} · ${t.realName ?? t.username}`),
        value: (t.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_117));
}
var __VLS_115;
var __VLS_111;
const __VLS_120 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    label: "规则类型",
}));
const __VLS_122 = __VLS_121({
    label: "规则类型",
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
const __VLS_124 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    modelValue: (__VLS_ctx.form.ruleType),
}));
const __VLS_126 = __VLS_125({
    modelValue: (__VLS_ctx.form.ruleType),
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
__VLS_127.slots.default;
const __VLS_128 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    value: "FixedAmount",
}));
const __VLS_130 = __VLS_129({
    value: "FixedAmount",
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_131.slots.default;
var __VLS_131;
const __VLS_132 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    value: "Percentage",
}));
const __VLS_134 = __VLS_133({
    value: "Percentage",
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
__VLS_135.slots.default;
var __VLS_135;
const __VLS_136 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
    value: "Tiered",
}));
const __VLS_138 = __VLS_137({
    value: "Tiered",
}, ...__VLS_functionalComponentArgsRest(__VLS_137));
__VLS_139.slots.default;
var __VLS_139;
const __VLS_140 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    value: "Timed",
}));
const __VLS_142 = __VLS_141({
    value: "Timed",
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
__VLS_143.slots.default;
var __VLS_143;
var __VLS_127;
var __VLS_123;
const __VLS_144 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    label: (__VLS_ctx.amountLabel),
}));
const __VLS_146 = __VLS_145({
    label: (__VLS_ctx.amountLabel),
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
__VLS_147.slots.default;
const __VLS_148 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    modelValue: (__VLS_ctx.form.amount),
    min: (0),
    precision: (2),
    step: (__VLS_ctx.form.ruleType === 'Percentage' ? 1 : 5),
}));
const __VLS_150 = __VLS_149({
    modelValue: (__VLS_ctx.form.amount),
    min: (0),
    precision: (2),
    step: (__VLS_ctx.form.ruleType === 'Percentage' ? 1 : 5),
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "muted" },
    ...{ style: {} },
});
(__VLS_ctx.amountHint);
var __VLS_147;
if (__VLS_ctx.form.ruleType === 'Tiered') {
    const __VLS_152 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
        label: "阶梯配置",
    }));
    const __VLS_154 = __VLS_153({
        label: "阶梯配置",
    }, ...__VLS_functionalComponentArgsRest(__VLS_153));
    __VLS_155.slots.default;
    const __VLS_156 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
        modelValue: (__VLS_ctx.form.tieredRulesJson),
        type: "textarea",
        rows: (4),
        placeholder: '[{"FromQty":0,"Amount":20},{"FromQty":31,"Amount":30}]',
    }));
    const __VLS_158 = __VLS_157({
        modelValue: (__VLS_ctx.form.tieredRulesJson),
        type: "textarea",
        rows: (4),
        placeholder: '[{"FromQty":0,"Amount":20},{"FromQty":31,"Amount":30}]',
    }, ...__VLS_functionalComponentArgsRest(__VLS_157));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "muted" },
        ...{ style: {} },
    });
    var __VLS_155;
}
const __VLS_160 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
    label: "优先级",
}));
const __VLS_162 = __VLS_161({
    label: "优先级",
}, ...__VLS_functionalComponentArgsRest(__VLS_161));
__VLS_163.slots.default;
const __VLS_164 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    modelValue: (__VLS_ctx.form.priority),
    min: (0),
    max: (100),
}));
const __VLS_166 = __VLS_165({
    modelValue: (__VLS_ctx.form.priority),
    min: (0),
    max: (100),
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "muted" },
    ...{ style: {} },
});
var __VLS_163;
const __VLS_168 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
    label: "启用",
}));
const __VLS_170 = __VLS_169({
    label: "启用",
}, ...__VLS_functionalComponentArgsRest(__VLS_169));
__VLS_171.slots.default;
const __VLS_172 = {}.ElSwitch;
/** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
// @ts-ignore
const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
    modelValue: (__VLS_ctx.form.isActive),
}));
const __VLS_174 = __VLS_173({
    modelValue: (__VLS_ctx.form.isActive),
}, ...__VLS_functionalComponentArgsRest(__VLS_173));
var __VLS_171;
var __VLS_95;
{
    const { footer: __VLS_thisSlot } = __VLS_91.slots;
    const __VLS_176 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
        ...{ 'onClick': {} },
    }));
    const __VLS_178 = __VLS_177({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_177));
    let __VLS_180;
    let __VLS_181;
    let __VLS_182;
    const __VLS_183 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_179.slots.default;
    var __VLS_179;
    const __VLS_184 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_186 = __VLS_185({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_185));
    let __VLS_188;
    let __VLS_189;
    let __VLS_190;
    const __VLS_191 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_187.slots.default;
    var __VLS_187;
}
var __VLS_91;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            rows: rows,
            services: services,
            technicians: technicians,
            loading: loading,
            saving: saving,
            filterServiceId: filterServiceId,
            filterTechnicianId: filterTechnicianId,
            formOpen: formOpen,
            formMode: formMode,
            form: form,
            ruleLabel: ruleLabel,
            formatAmount: formatAmount,
            amountLabel: amountLabel,
            amountHint: amountHint,
            reload: reload,
            openCreate: openCreate,
            openEdit: openEdit,
            save: save,
            onDelete: onDelete,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
