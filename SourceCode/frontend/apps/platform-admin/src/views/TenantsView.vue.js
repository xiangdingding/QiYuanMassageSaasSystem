import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { plansApi, tenantsApi } from '@/api/modules';
import CreateTenantDialog from '@/views/components/CreateTenantDialog.vue';
import OfflineActivateDialog from '@/views/components/OfflineActivateDialog.vue';
import TenantOverviewDialog from '@/views/components/TenantOverviewDialog.vue';
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const plans = ref([]);
const query = reactive({
    page: 1,
    pageSize: 20,
    keyword: '',
    status: ''
});
const createOpen = ref(false);
const activateOpen = ref(false);
const activateTarget = ref(null);
const activateMode = ref('activate');
const overviewOpen = ref(false);
const overviewTarget = ref(null);
function statusType(s) {
    if (s === 'Active')
        return 'success';
    if (s === 'Expired')
        return 'warning';
    return 'danger';
}
function statusLabel(s) {
    return { Active: '活跃', Expired: '已过期', Disabled: '已停用' }[s] ?? s;
}
function formatDate(v) {
    return new Date(v).toLocaleDateString('zh-CN');
}
async function reload() {
    loading.value = true;
    try {
        const data = await tenantsApi.list({
            page: query.page,
            pageSize: query.pageSize,
            keyword: query.keyword || undefined,
            status: query.status || undefined
        });
        rows.value = data.items;
        total.value = data.total;
    }
    finally {
        loading.value = false;
    }
}
function resetQuery() {
    query.keyword = '';
    query.status = '';
    query.page = 1;
    reload();
}
function openCreate() {
    createOpen.value = true;
}
function onCreated() {
    createOpen.value = false;
    query.page = 1;
    reload();
}
function openActivate(row, mode) {
    activateTarget.value = row;
    activateMode.value = mode;
    activateOpen.value = true;
}
async function removeTenant(row) {
    const ok = await ElMessageBox.confirm(`确认删除未激活的租户「${row.name}」？将同时清除自动创建的总店与店主账号，且不可恢复。`, '请确认', { type: 'warning', confirmButtonText: '删除', confirmButtonClass: 'el-button--danger' }).catch(() => null);
    if (!ok)
        return;
    try {
        await tenantsApi.remove(row.id);
        ElMessage.success('已删除');
        reload();
    }
    catch {
        /* http interceptor surfaces error */
    }
}
function openOverview(row) {
    overviewTarget.value = row;
    overviewOpen.value = true;
}
async function changeStatus(row, status) {
    const verb = status === 'Disabled' ? '停用' : '启用';
    await ElMessageBox.confirm(`确认${verb}租户「${row.name}」？`, '请确认', { type: 'warning' }).catch(() => null);
    try {
        await tenantsApi.updateStatus(row.id, status);
        ElMessage.success(`已${verb}`);
        reload();
    }
    catch {
        /* http interceptor surfaces error */
    }
}
onMounted(async () => {
    plans.value = await plansApi.list(false).catch(() => []);
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
const __VLS_4 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.query.keyword),
    placeholder: "搜索店名 / 联系电话",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.query.keyword),
    placeholder: "搜索店名 / 联系电话",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onKeyup: (__VLS_ctx.reload)
};
var __VLS_7;
const __VLS_12 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    modelValue: (__VLS_ctx.query.status),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_14 = __VLS_13({
    modelValue: (__VLS_ctx.query.status),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
__VLS_15.slots.default;
const __VLS_16 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    label: "活跃",
    value: "Active",
}));
const __VLS_18 = __VLS_17({
    label: "活跃",
    value: "Active",
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
    label: "已停用",
    value: "Disabled",
}));
const __VLS_26 = __VLS_25({
    label: "已停用",
    value: "Disabled",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
var __VLS_15;
const __VLS_28 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_30 = __VLS_29({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_31.slots.default;
var __VLS_31;
const __VLS_36 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    ...{ 'onClick': {} },
}));
const __VLS_38 = __VLS_37({
    ...{ 'onClick': {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
let __VLS_40;
let __VLS_41;
let __VLS_42;
const __VLS_43 = {
    onClick: (__VLS_ctx.resetQuery)
};
__VLS_39.slots.default;
var __VLS_39;
const __VLS_44 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    ...{ 'onClick': {} },
    type: "success",
}));
const __VLS_46 = __VLS_45({
    ...{ 'onClick': {} },
    type: "success",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
let __VLS_48;
let __VLS_49;
let __VLS_50;
const __VLS_51 = {
    onClick: (__VLS_ctx.openCreate)
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
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "name",
    label: "店名",
    minWidth: "180",
}));
const __VLS_58 = __VLS_57({
    prop: "name",
    label: "店名",
    minWidth: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    prop: "contactPhone",
    label: "联系电话",
    minWidth: "120",
}));
const __VLS_62 = __VLS_61({
    prop: "contactPhone",
    label: "联系电话",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    prop: "contactName",
    label: "联系人",
    minWidth: "100",
}));
const __VLS_66 = __VLS_65({
    prop: "contactName",
    label: "联系人",
    minWidth: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    prop: "currentPlanName",
    label: "当前套餐",
    minWidth: "120",
}));
const __VLS_70 = __VLS_69({
    prop: "currentPlanName",
    label: "当前套餐",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
__VLS_71.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_71.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.currentPlanName ?? '—');
}
var __VLS_71;
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    label: "状态",
    minWidth: "100",
}));
const __VLS_74 = __VLS_73({
    label: "状态",
    minWidth: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_75.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_76 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
        type: (__VLS_ctx.statusType(row.status)),
    }));
    const __VLS_78 = __VLS_77({
        type: (__VLS_ctx.statusType(row.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_77));
    __VLS_79.slots.default;
    (__VLS_ctx.statusLabel(row.status));
    var __VLS_79;
}
var __VLS_75;
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    label: "订阅开始",
    minWidth: "120",
}));
const __VLS_82 = __VLS_81({
    label: "订阅开始",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
__VLS_83.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_83.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (!row.subscriptionStartAt) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (__VLS_ctx.formatDate(row.subscriptionStartAt));
    }
}
var __VLS_83;
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    label: "订阅年限",
    width: "100",
    align: "center",
}));
const __VLS_86 = __VLS_85({
    label: "订阅年限",
    width: "100",
    align: "center",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.subscriptionYears == null) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (row.subscriptionYears);
    }
}
var __VLS_87;
const __VLS_88 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    label: "到期时间",
    minWidth: "180",
}));
const __VLS_90 = __VLS_89({
    label: "到期时间",
    minWidth: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
__VLS_91.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_91.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (!row.expireAt) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (__VLS_ctx.formatDate(row.expireAt));
        if (row.daysToExpire != null && row.daysToExpire <= 30 && row.daysToExpire > 0) {
            const __VLS_92 = {}.ElTag;
            /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
            // @ts-ignore
            const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
                size: "small",
                type: "warning",
                effect: "plain",
            }));
            const __VLS_94 = __VLS_93({
                size: "small",
                type: "warning",
                effect: "plain",
            }, ...__VLS_functionalComponentArgsRest(__VLS_93));
            __VLS_95.slots.default;
            (row.daysToExpire);
            var __VLS_95;
        }
    }
}
var __VLS_91;
const __VLS_96 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    label: "操作",
    width: "380",
    fixed: "right",
}));
const __VLS_98 = __VLS_97({
    label: "操作",
    width: "380",
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
        link: true,
        type: "primary",
    }));
    const __VLS_102 = __VLS_101({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
    let __VLS_104;
    let __VLS_105;
    let __VLS_106;
    const __VLS_107 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openOverview(row);
        }
    };
    __VLS_103.slots.default;
    var __VLS_103;
    if (!row.subscriptionStartAt) {
        const __VLS_108 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
            ...{ 'onClick': {} },
            link: true,
            type: "success",
        }));
        const __VLS_110 = __VLS_109({
            ...{ 'onClick': {} },
            link: true,
            type: "success",
        }, ...__VLS_functionalComponentArgsRest(__VLS_109));
        let __VLS_112;
        let __VLS_113;
        let __VLS_114;
        const __VLS_115 = {
            onClick: (...[$event]) => {
                if (!(!row.subscriptionStartAt))
                    return;
                __VLS_ctx.openActivate(row, 'activate');
            }
        };
        __VLS_111.slots.default;
        var __VLS_111;
    }
    else {
        const __VLS_116 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
            ...{ 'onClick': {} },
            link: true,
            type: "primary",
        }));
        const __VLS_118 = __VLS_117({
            ...{ 'onClick': {} },
            link: true,
            type: "primary",
        }, ...__VLS_functionalComponentArgsRest(__VLS_117));
        let __VLS_120;
        let __VLS_121;
        let __VLS_122;
        const __VLS_123 = {
            onClick: (...[$event]) => {
                if (!!(!row.subscriptionStartAt))
                    return;
                __VLS_ctx.openActivate(row, 'renew');
            }
        };
        __VLS_119.slots.default;
        var __VLS_119;
    }
    if (!row.subscriptionStartAt) {
        const __VLS_124 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }));
        const __VLS_126 = __VLS_125({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }, ...__VLS_functionalComponentArgsRest(__VLS_125));
        let __VLS_128;
        let __VLS_129;
        let __VLS_130;
        const __VLS_131 = {
            onClick: (...[$event]) => {
                if (!(!row.subscriptionStartAt))
                    return;
                __VLS_ctx.removeTenant(row);
            }
        };
        __VLS_127.slots.default;
        var __VLS_127;
    }
    else if (row.status !== 'Disabled') {
        const __VLS_132 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }));
        const __VLS_134 = __VLS_133({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }, ...__VLS_functionalComponentArgsRest(__VLS_133));
        let __VLS_136;
        let __VLS_137;
        let __VLS_138;
        const __VLS_139 = {
            onClick: (...[$event]) => {
                if (!!(!row.subscriptionStartAt))
                    return;
                if (!(row.status !== 'Disabled'))
                    return;
                __VLS_ctx.changeStatus(row, 'Disabled');
            }
        };
        __VLS_135.slots.default;
        var __VLS_135;
    }
    else {
        const __VLS_140 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
            ...{ 'onClick': {} },
            link: true,
            type: "success",
        }));
        const __VLS_142 = __VLS_141({
            ...{ 'onClick': {} },
            link: true,
            type: "success",
        }, ...__VLS_functionalComponentArgsRest(__VLS_141));
        let __VLS_144;
        let __VLS_145;
        let __VLS_146;
        const __VLS_147 = {
            onClick: (...[$event]) => {
                if (!!(!row.subscriptionStartAt))
                    return;
                if (!!(row.status !== 'Disabled'))
                    return;
                __VLS_ctx.changeStatus(row, 'Active');
            }
        };
        __VLS_143.slots.default;
        var __VLS_143;
    }
}
var __VLS_99;
var __VLS_55;
const __VLS_148 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}));
const __VLS_150 = __VLS_149({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
let __VLS_152;
let __VLS_153;
let __VLS_154;
const __VLS_155 = {
    onCurrentChange: ((p) => { __VLS_ctx.query.page = p; __VLS_ctx.reload(); })
};
const __VLS_156 = {
    onSizeChange: ((s) => { __VLS_ctx.query.pageSize = s; __VLS_ctx.query.page = 1; __VLS_ctx.reload(); })
};
var __VLS_151;
var __VLS_3;
/** @type {[typeof CreateTenantDialog, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(CreateTenantDialog, new CreateTenantDialog({
    ...{ 'onCreated': {} },
    modelValue: (__VLS_ctx.createOpen),
}));
const __VLS_158 = __VLS_157({
    ...{ 'onCreated': {} },
    modelValue: (__VLS_ctx.createOpen),
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
let __VLS_160;
let __VLS_161;
let __VLS_162;
const __VLS_163 = {
    onCreated: (__VLS_ctx.onCreated)
};
var __VLS_159;
/** @type {[typeof OfflineActivateDialog, ]} */ ;
// @ts-ignore
const __VLS_164 = __VLS_asFunctionalComponent(OfflineActivateDialog, new OfflineActivateDialog({
    ...{ 'onActivated': {} },
    modelValue: (__VLS_ctx.activateOpen),
    tenant: (__VLS_ctx.activateTarget),
    plans: (__VLS_ctx.plans),
    mode: (__VLS_ctx.activateMode),
}));
const __VLS_165 = __VLS_164({
    ...{ 'onActivated': {} },
    modelValue: (__VLS_ctx.activateOpen),
    tenant: (__VLS_ctx.activateTarget),
    plans: (__VLS_ctx.plans),
    mode: (__VLS_ctx.activateMode),
}, ...__VLS_functionalComponentArgsRest(__VLS_164));
let __VLS_167;
let __VLS_168;
let __VLS_169;
const __VLS_170 = {
    onActivated: (__VLS_ctx.reload)
};
var __VLS_166;
/** @type {[typeof TenantOverviewDialog, ]} */ ;
// @ts-ignore
const __VLS_171 = __VLS_asFunctionalComponent(TenantOverviewDialog, new TenantOverviewDialog({
    modelValue: (__VLS_ctx.overviewOpen),
    tenant: (__VLS_ctx.overviewTarget),
}));
const __VLS_172 = __VLS_171({
    modelValue: (__VLS_ctx.overviewOpen),
    tenant: (__VLS_ctx.overviewTarget),
}, ...__VLS_functionalComponentArgsRest(__VLS_171));
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            CreateTenantDialog: CreateTenantDialog,
            OfflineActivateDialog: OfflineActivateDialog,
            TenantOverviewDialog: TenantOverviewDialog,
            rows: rows,
            total: total,
            loading: loading,
            plans: plans,
            query: query,
            createOpen: createOpen,
            activateOpen: activateOpen,
            activateTarget: activateTarget,
            activateMode: activateMode,
            overviewOpen: overviewOpen,
            overviewTarget: overviewTarget,
            statusType: statusType,
            statusLabel: statusLabel,
            formatDate: formatDate,
            reload: reload,
            resetQuery: resetQuery,
            openCreate: openCreate,
            onCreated: onCreated,
            openActivate: openActivate,
            removeTenant: removeTenant,
            openOverview: openOverview,
            changeStatus: changeStatus,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
