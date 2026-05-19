import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { memberPackagesApi, membersApi, servicesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const loading = ref(false);
const statusFilter = ref(undefined);
const formOpen = ref(false);
const saving = ref(false);
const memberSearching = ref(false);
const memberOptions = ref([]);
const serviceList = ref([]);
const form = reactive({
    memberId: 0, kind: 'Counter',
    serviceId: null,
    title: '', paidAmount: 0, totalCount: 10,
    validFrom: null, expiresAt: null,
    remark: ''
});
async function reload() {
    loading.value = true;
    try {
        rows.value = await memberPackagesApi.list({
            storeId: appStore.activeStoreId ?? undefined,
            status: statusFilter.value
        });
    }
    finally {
        loading.value = false;
    }
}
async function searchMembers(q) {
    if (!q)
        return;
    memberSearching.value = true;
    try {
        const data = await membersApi.list({ keyword: q, page: 1, pageSize: 20 });
        memberOptions.value = data.items;
    }
    finally {
        memberSearching.value = false;
    }
}
function openNew() {
    Object.assign(form, {
        memberId: 0, kind: 'Counter', serviceId: null,
        title: '', paidAmount: 0, totalCount: 10,
        validFrom: null, expiresAt: null, remark: ''
    });
    formOpen.value = true;
}
async function save() {
    if (!form.memberId) {
        ElMessage.warning('请选择会员');
        return;
    }
    if (form.kind === 'Counter' && (!form.serviceId || form.totalCount < 1)) {
        ElMessage.warning('计次卡需选择服务且总次数 >= 1');
        return;
    }
    saving.value = true;
    try {
        await memberPackagesApi.create({
            memberId: form.memberId, storeId: appStore.activeStoreId,
            kind: form.kind, serviceId: form.serviceId,
            title: form.title.trim(), paidAmount: form.paidAmount,
            totalCount: form.totalCount,
            validFrom: form.validFrom, expiresAt: form.expiresAt,
            remark: form.remark || null
        });
        formOpen.value = false;
        ElMessage.success('已售卡');
        await reload();
    }
    finally {
        saving.value = false;
    }
}
async function cancel(row) {
    await ElMessageBox.confirm(`确认作废套餐"${row.title}"？`, '提示', { type: 'warning' }).catch(() => null);
    await memberPackagesApi.cancel(row.id);
    ElMessage.success('已作废');
    await reload();
}
onMounted(async () => {
    serviceList.value = await servicesApi.list();
    await reload();
});
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
    label: "已用完",
    value: "Used",
}));
const __VLS_18 = __VLS_17({
    label: "已用完",
    value: "Used",
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_28 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
}));
const __VLS_30 = __VLS_29({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onClick: (__VLS_ctx.openNew)
};
__VLS_31.slots.default;
var __VLS_31;
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
    prop: "memberName",
    label: "会员",
    width: "120",
}));
const __VLS_50 = __VLS_49({
    prop: "memberName",
    label: "会员",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "title",
    label: "套餐",
    minWidth: "160",
}));
const __VLS_54 = __VLS_53({
    prop: "title",
    label: "套餐",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
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
    (row.kind === 'Counter' ? '计次卡' : '期限卡');
}
var __VLS_59;
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    prop: "serviceName",
    label: "服务",
    width: "140",
}));
const __VLS_62 = __VLS_61({
    prop: "serviceName",
    label: "服务",
    width: "140",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.serviceName || '不限');
}
var __VLS_63;
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    label: "次数",
    width: "100",
}));
const __VLS_66 = __VLS_65({
    label: "次数",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_67.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_67.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.remainCount);
    (row.totalCount);
}
var __VLS_67;
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    prop: "paidAmount",
    label: "售价",
    width: "100",
}));
const __VLS_70 = __VLS_69({
    prop: "paidAmount",
    label: "售价",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    prop: "expiresAt",
    label: "到期",
    width: "160",
}));
const __VLS_74 = __VLS_73({
    prop: "expiresAt",
    label: "到期",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_75.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.expiresAt || '永不');
}
var __VLS_75;
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    prop: "status",
    label: "状态",
    width: "100",
}));
const __VLS_78 = __VLS_77({
    prop: "status",
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    label: "操作",
    width: "120",
    fixed: "right",
}));
const __VLS_82 = __VLS_81({
    label: "操作",
    width: "120",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
__VLS_83.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_83.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_84 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Active'),
    }));
    const __VLS_86 = __VLS_85({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Active'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_85));
    let __VLS_88;
    let __VLS_89;
    let __VLS_90;
    const __VLS_91 = {
        onClick: (...[$event]) => {
            __VLS_ctx.cancel(row);
        }
    };
    __VLS_87.slots.default;
    var __VLS_87;
}
var __VLS_83;
var __VLS_47;
var __VLS_3;
const __VLS_92 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    modelValue: (__VLS_ctx.formOpen),
    title: "售卖会员套餐",
    width: "480px",
}));
const __VLS_94 = __VLS_93({
    modelValue: (__VLS_ctx.formOpen),
    title: "售卖会员套餐",
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
__VLS_95.slots.default;
const __VLS_96 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    model: (__VLS_ctx.form),
    labelWidth: "100px",
}));
const __VLS_98 = __VLS_97({
    model: (__VLS_ctx.form),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
__VLS_99.slots.default;
const __VLS_100 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    label: "会员",
    required: true,
}));
const __VLS_102 = __VLS_101({
    label: "会员",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_103.slots.default;
const __VLS_104 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
    modelValue: (__VLS_ctx.form.memberId),
    filterable: true,
    remote: true,
    remoteMethod: (__VLS_ctx.searchMembers),
    loading: (__VLS_ctx.memberSearching),
    placeholder: "按卡号/手机号搜索",
    ...{ style: {} },
}));
const __VLS_106 = __VLS_105({
    modelValue: (__VLS_ctx.form.memberId),
    filterable: true,
    remote: true,
    remoteMethod: (__VLS_ctx.searchMembers),
    loading: (__VLS_ctx.memberSearching),
    placeholder: "按卡号/手机号搜索",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_105));
__VLS_107.slots.default;
for (const [m] of __VLS_getVForSourceType((__VLS_ctx.memberOptions))) {
    const __VLS_108 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
        key: (m.id),
        label: (`${m.cardNo}（${m.name || m.phone}）`),
        value: (m.id),
    }));
    const __VLS_110 = __VLS_109({
        key: (m.id),
        label: (`${m.cardNo}（${m.name || m.phone}）`),
        value: (m.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_109));
}
var __VLS_107;
var __VLS_103;
const __VLS_112 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    label: "类型",
    required: true,
}));
const __VLS_114 = __VLS_113({
    label: "类型",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
__VLS_115.slots.default;
const __VLS_116 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
    modelValue: (__VLS_ctx.form.kind),
}));
const __VLS_118 = __VLS_117({
    modelValue: (__VLS_ctx.form.kind),
}, ...__VLS_functionalComponentArgsRest(__VLS_117));
__VLS_119.slots.default;
const __VLS_120 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    value: "Counter",
}));
const __VLS_122 = __VLS_121({
    value: "Counter",
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
var __VLS_123;
const __VLS_124 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    value: "Period",
}));
const __VLS_126 = __VLS_125({
    value: "Period",
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
__VLS_127.slots.default;
var __VLS_127;
var __VLS_119;
var __VLS_115;
const __VLS_128 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    label: "服务",
    required: (__VLS_ctx.form.kind === 'Counter'),
}));
const __VLS_130 = __VLS_129({
    label: "服务",
    required: (__VLS_ctx.form.kind === 'Counter'),
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_131.slots.default;
const __VLS_132 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    modelValue: (__VLS_ctx.form.serviceId),
    filterable: true,
    clearable: true,
    placeholder: "选择服务",
    ...{ style: {} },
}));
const __VLS_134 = __VLS_133({
    modelValue: (__VLS_ctx.form.serviceId),
    filterable: true,
    clearable: true,
    placeholder: "选择服务",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
__VLS_135.slots.default;
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.serviceList))) {
    const __VLS_136 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
        key: (s.id),
        label: (s.name),
        value: (s.id),
    }));
    const __VLS_138 = __VLS_137({
        key: (s.id),
        label: (s.name),
        value: (s.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_137));
}
var __VLS_135;
var __VLS_131;
const __VLS_140 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    label: "套餐名称",
    required: true,
}));
const __VLS_142 = __VLS_141({
    label: "套餐名称",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
__VLS_143.slots.default;
const __VLS_144 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    modelValue: (__VLS_ctx.form.title),
    maxlength: "100",
}));
const __VLS_146 = __VLS_145({
    modelValue: (__VLS_ctx.form.title),
    maxlength: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
var __VLS_143;
const __VLS_148 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    label: "售价",
}));
const __VLS_150 = __VLS_149({
    label: "售价",
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
__VLS_151.slots.default;
const __VLS_152 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
    modelValue: (__VLS_ctx.form.paidAmount),
    min: (0),
    precision: (2),
}));
const __VLS_154 = __VLS_153({
    modelValue: (__VLS_ctx.form.paidAmount),
    min: (0),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_153));
var __VLS_151;
const __VLS_156 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
    label: "总次数",
}));
const __VLS_158 = __VLS_157({
    label: "总次数",
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
__VLS_159.slots.default;
const __VLS_160 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
    modelValue: (__VLS_ctx.form.totalCount),
    min: (0),
}));
const __VLS_162 = __VLS_161({
    modelValue: (__VLS_ctx.form.totalCount),
    min: (0),
}, ...__VLS_functionalComponentArgsRest(__VLS_161));
var __VLS_159;
const __VLS_164 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    label: "生效起",
}));
const __VLS_166 = __VLS_165({
    label: "生效起",
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_167.slots.default;
const __VLS_168 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
    modelValue: (__VLS_ctx.form.validFrom),
    type: "datetime",
}));
const __VLS_170 = __VLS_169({
    modelValue: (__VLS_ctx.form.validFrom),
    type: "datetime",
}, ...__VLS_functionalComponentArgsRest(__VLS_169));
var __VLS_167;
const __VLS_172 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
    label: "到期",
}));
const __VLS_174 = __VLS_173({
    label: "到期",
}, ...__VLS_functionalComponentArgsRest(__VLS_173));
__VLS_175.slots.default;
const __VLS_176 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
    modelValue: (__VLS_ctx.form.expiresAt),
    type: "datetime",
}));
const __VLS_178 = __VLS_177({
    modelValue: (__VLS_ctx.form.expiresAt),
    type: "datetime",
}, ...__VLS_functionalComponentArgsRest(__VLS_177));
var __VLS_175;
const __VLS_180 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
    label: "备注",
}));
const __VLS_182 = __VLS_181({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_181));
__VLS_183.slots.default;
const __VLS_184 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_186 = __VLS_185({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_185));
var __VLS_183;
var __VLS_99;
{
    const { footer: __VLS_thisSlot } = __VLS_95.slots;
    const __VLS_188 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_189 = __VLS_asFunctionalComponent(__VLS_188, new __VLS_188({
        ...{ 'onClick': {} },
    }));
    const __VLS_190 = __VLS_189({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_189));
    let __VLS_192;
    let __VLS_193;
    let __VLS_194;
    const __VLS_195 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_191.slots.default;
    var __VLS_191;
    const __VLS_196 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_197 = __VLS_asFunctionalComponent(__VLS_196, new __VLS_196({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_198 = __VLS_197({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_197));
    let __VLS_200;
    let __VLS_201;
    let __VLS_202;
    const __VLS_203 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_199.slots.default;
    var __VLS_199;
}
var __VLS_95;
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
            formOpen: formOpen,
            saving: saving,
            memberSearching: memberSearching,
            memberOptions: memberOptions,
            serviceList: serviceList,
            form: form,
            reload: reload,
            searchMembers: searchMembers,
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
