import { computed, onMounted, reactive, ref, watch } from 'vue';
import dayjs from 'dayjs';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { payrollApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const tab = ref('periods');
const periods = ref([]);
const profiles = ref([]);
const loading = ref(false);
const saving = ref(false);
const generating = ref(false);
const yearFilter = ref(dayjs().format('YYYY'));
const genMonth = ref(dayjs().format('YYYY-MM'));
const detailOpen = ref(false);
const detail = ref(null);
const expandedItem = ref(null);
const editItemOpen = ref(false);
const editingItem = ref(null);
const editForm = reactive({ overtimeHours: 0, attendanceBonusOverride: -1, remark: '' });
const addAdjOpen = ref(false);
const adjTarget = ref(null);
const adjForm = reactive({ kind: 'Bonus', amount: 0, reason: '' });
const profileOpen = ref(false);
const editingProfile = ref(null);
const profileForm = reactive({
    baseMonthly: 0, overtimeHourRate: 0,
    attendanceBonusAmount: 0, requiredAttendanceDays: 0, remark: ''
});
const activeStoreName = computed(() => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? '');
const detailTitle = computed(() => detail.value ? `${detail.value.period.year}-${String(detail.value.period.month).padStart(2, '0')} 工资单` : '');
function statusLabel(s) {
    return { Draft: '草稿', Locked: '已封盘', Paid: '已发放' }[s] ?? s;
}
function statusTag(s) {
    return s === 'Draft' ? 'info' : s === 'Locked' ? 'warning' : 'success';
}
async function loadPeriods() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        periods.value = await payrollApi.periods(appStore.activeStoreId, yearFilter.value ? Number(yearFilter.value) : undefined);
    }
    finally {
        loading.value = false;
    }
}
async function loadProfiles() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        // 拿员工再合并 profile（员工有可能没设过）
        const staff = (await staffApi.list({ storeId: appStore.activeStoreId, page: 1, pageSize: 200 })).items;
        const profileMap = new Map((await payrollApi.profiles(appStore.activeStoreId)).map((p) => [p.userId, p]));
        profiles.value = staff.map((u) => profileMap.get(u.id) ?? ({
            userId: u.id, userName: u.realName ?? u.username,
            baseMonthly: 0, overtimeHourRate: 0,
            attendanceBonusAmount: 0, requiredAttendanceDays: 0, remark: null
        }));
    }
    finally {
        loading.value = false;
    }
}
async function generate() {
    if (!appStore.activeStoreId)
        return;
    const [y, m] = genMonth.value.split('-').map(Number);
    generating.value = true;
    try {
        await payrollApi.generate(appStore.activeStoreId, y, m, null);
        ElMessage.success('已生成草稿，请进入查看明细');
        await loadPeriods();
    }
    finally {
        generating.value = false;
    }
}
async function openDetail(row) {
    detail.value = await payrollApi.period(row.id);
    expandedItem.value = null;
    detailOpen.value = true;
}
async function lockPeriod(row) {
    await ElMessageBox.confirm(`封盘后该月工资单不可再修改。确认封盘 ${row.year}-${row.month}？`, '提示', { type: 'warning' }).catch(() => null);
    await payrollApi.lock(row.id);
    ElMessage.success('已封盘');
    await loadPeriods();
}
async function markPaid(row) {
    await ElMessageBox.confirm(`确认 ${row.year}-${row.month} 已发放工资？`, '提示').catch(() => null);
    await payrollApi.markPaid(row.id);
    ElMessage.success('已标记发放');
    await loadPeriods();
}
async function removeDraft(row) {
    await ElMessageBox.confirm(`确认删除 ${row.year}-${row.month} 工资单草稿？`, '提示', { type: 'warning' }).catch(() => null);
    await payrollApi.removeDraft(row.id);
    ElMessage.success('已删除');
    await loadPeriods();
}
function openEditItem(item) {
    editingItem.value = item;
    editForm.overtimeHours = item.overtimeHours;
    editForm.attendanceBonusOverride = -1;
    editForm.remark = item.remark ?? '';
    editItemOpen.value = true;
}
async function saveItem() {
    if (!editingItem.value || !detail.value)
        return;
    saving.value = true;
    try {
        const updated = await payrollApi.updateItem(editingItem.value.id, editForm.overtimeHours, editForm.attendanceBonusOverride, editForm.remark || null);
        mergeItem(updated);
        editItemOpen.value = false;
        ElMessage.success('已保存');
        await refreshTotals();
    }
    finally {
        saving.value = false;
    }
}
function openAddAdj(item) {
    adjTarget.value = item;
    expandedItem.value = item;
    adjForm.kind = 'Bonus';
    adjForm.amount = 0;
    adjForm.reason = '';
    addAdjOpen.value = true;
}
async function saveAdj() {
    if (!adjTarget.value)
        return;
    if (adjForm.amount <= 0 || !adjForm.reason.trim()) {
        ElMessage.warning('金额与原因必填');
        return;
    }
    saving.value = true;
    try {
        const updated = await payrollApi.addAdjustment(adjTarget.value.id, adjForm.kind, adjForm.amount, adjForm.reason.trim());
        mergeItem(updated);
        expandedItem.value = updated;
        addAdjOpen.value = false;
        ElMessage.success('已新增');
        await refreshTotals();
    }
    finally {
        saving.value = false;
    }
}
async function removeAdj(itemId, adjId) {
    const updated = await payrollApi.removeAdjustment(itemId, adjId);
    mergeItem(updated);
    if (expandedItem.value?.id === itemId)
        expandedItem.value = updated;
    ElMessage.success('已删除');
    await refreshTotals();
}
function mergeItem(updated) {
    if (!detail.value)
        return;
    const idx = detail.value.items.findIndex((i) => i.id === updated.id);
    if (idx >= 0)
        detail.value.items.splice(idx, 1, updated);
}
async function refreshTotals() {
    if (!detail.value)
        return;
    detail.value.period.totalAmount = detail.value.items.reduce((a, b) => a + b.netTotal, 0);
}
function openProfile(p) {
    editingProfile.value = p;
    profileForm.baseMonthly = p.baseMonthly;
    profileForm.overtimeHourRate = p.overtimeHourRate;
    profileForm.attendanceBonusAmount = p.attendanceBonusAmount;
    profileForm.requiredAttendanceDays = p.requiredAttendanceDays;
    profileForm.remark = p.remark ?? '';
    profileOpen.value = true;
}
async function saveProfile() {
    if (!editingProfile.value)
        return;
    saving.value = true;
    try {
        await payrollApi.upsertProfile(editingProfile.value.userId, {
            baseMonthly: profileForm.baseMonthly,
            overtimeHourRate: profileForm.overtimeHourRate,
            attendanceBonusAmount: profileForm.attendanceBonusAmount,
            requiredAttendanceDays: profileForm.requiredAttendanceDays,
            remark: profileForm.remark || null
        });
        profileOpen.value = false;
        ElMessage.success('已保存');
        await loadProfiles();
    }
    finally {
        saving.value = false;
    }
}
watch(() => appStore.activeStoreId, () => {
    if (tab.value === 'periods')
        loadPeriods();
    else
        loadProfiles();
});
watch(tab, (t) => {
    if (t === 'periods')
        loadPeriods();
    else
        loadProfiles();
});
onMounted(async () => {
    await appStore.loadStores();
    await loadPeriods();
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['adj-section']} */ ;
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
    label: "工资单",
    name: "periods",
}));
const __VLS_6 = __VLS_5({
    label: "工资单",
    name: "periods",
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "title" },
});
(__VLS_ctx.activeStoreName);
const __VLS_12 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.yearFilter),
    type: "year",
    format: "YYYY",
    valueFormat: "YYYY",
    placeholder: "年份",
    ...{ style: {} },
}));
const __VLS_14 = __VLS_13({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.yearFilter),
    type: "year",
    format: "YYYY",
    valueFormat: "YYYY",
    placeholder: "年份",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
let __VLS_16;
let __VLS_17;
let __VLS_18;
const __VLS_19 = {
    onChange: (__VLS_ctx.loadPeriods)
};
var __VLS_15;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_20 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    modelValue: (__VLS_ctx.genMonth),
    type: "month",
    format: "YYYY-MM",
    valueFormat: "YYYY-MM",
    placeholder: "月份",
    ...{ style: {} },
}));
const __VLS_22 = __VLS_21({
    modelValue: (__VLS_ctx.genMonth),
    type: "month",
    format: "YYYY-MM",
    valueFormat: "YYYY-MM",
    placeholder: "月份",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
const __VLS_24 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
    loading: (__VLS_ctx.generating),
}));
const __VLS_26 = __VLS_25({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
    loading: (__VLS_ctx.generating),
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
let __VLS_28;
let __VLS_29;
let __VLS_30;
const __VLS_31 = {
    onClick: (__VLS_ctx.generate)
};
__VLS_27.slots.default;
var __VLS_27;
const __VLS_32 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_34 = __VLS_33({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
let __VLS_36;
let __VLS_37;
let __VLS_38;
const __VLS_39 = {
    onClick: (__VLS_ctx.loadPeriods)
};
__VLS_35.slots.default;
var __VLS_35;
const __VLS_40 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    data: (__VLS_ctx.periods),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_42 = __VLS_41({
    data: (__VLS_ctx.periods),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_43.slots.default;
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    label: "月份",
    width: "120",
}));
const __VLS_46 = __VLS_45({
    label: "月份",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_47.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.year);
    (String(row.month).padStart(2, '0'));
}
var __VLS_47;
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    prop: "status",
    label: "状态",
    width: "100",
}));
const __VLS_50 = __VLS_49({
    prop: "status",
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
__VLS_51.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_51.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_52 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
        type: (__VLS_ctx.statusTag(row.status)),
    }));
    const __VLS_54 = __VLS_53({
        type: (__VLS_ctx.statusTag(row.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_53));
    __VLS_55.slots.default;
    (__VLS_ctx.statusLabel(row.status));
    var __VLS_55;
}
var __VLS_51;
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "itemCount",
    label: "人数",
    width: "80",
}));
const __VLS_58 = __VLS_57({
    prop: "itemCount",
    label: "人数",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "工资总额",
    width: "140",
}));
const __VLS_62 = __VLS_61({
    label: "工资总额",
    width: "140",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.totalAmount.toFixed(2));
}
var __VLS_63;
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    prop: "generatedAt",
    label: "生成时间",
    width: "180",
}));
const __VLS_66 = __VLS_65({
    prop: "generatedAt",
    label: "生成时间",
    width: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    prop: "operatorName",
    label: "操作人",
    width: "120",
}));
const __VLS_70 = __VLS_69({
    prop: "operatorName",
    label: "操作人",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    prop: "remark",
    label: "备注",
    minWidth: "160",
    showOverflowTooltip: true,
}));
const __VLS_74 = __VLS_73({
    prop: "remark",
    label: "备注",
    minWidth: "160",
    showOverflowTooltip: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    label: "操作",
    width: "280",
    fixed: "right",
}));
const __VLS_78 = __VLS_77({
    label: "操作",
    width: "280",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
__VLS_79.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_79.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_80 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
        ...{ 'onClick': {} },
        size: "small",
    }));
    const __VLS_82 = __VLS_81({
        ...{ 'onClick': {} },
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_81));
    let __VLS_84;
    let __VLS_85;
    let __VLS_86;
    const __VLS_87 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openDetail(row);
        }
    };
    __VLS_83.slots.default;
    var __VLS_83;
    const __VLS_88 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
        ...{ 'onClick': {} },
        size: "small",
        type: "warning",
        disabled: (row.status !== 'Draft'),
    }));
    const __VLS_90 = __VLS_89({
        ...{ 'onClick': {} },
        size: "small",
        type: "warning",
        disabled: (row.status !== 'Draft'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_89));
    let __VLS_92;
    let __VLS_93;
    let __VLS_94;
    const __VLS_95 = {
        onClick: (...[$event]) => {
            __VLS_ctx.lockPeriod(row);
        }
    };
    __VLS_91.slots.default;
    var __VLS_91;
    const __VLS_96 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
        ...{ 'onClick': {} },
        size: "small",
        type: "success",
        disabled: (row.status !== 'Locked'),
    }));
    const __VLS_98 = __VLS_97({
        ...{ 'onClick': {} },
        size: "small",
        type: "success",
        disabled: (row.status !== 'Locked'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_97));
    let __VLS_100;
    let __VLS_101;
    let __VLS_102;
    const __VLS_103 = {
        onClick: (...[$event]) => {
            __VLS_ctx.markPaid(row);
        }
    };
    __VLS_99.slots.default;
    var __VLS_99;
    const __VLS_104 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Draft'),
    }));
    const __VLS_106 = __VLS_105({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Draft'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_105));
    let __VLS_108;
    let __VLS_109;
    let __VLS_110;
    const __VLS_111 = {
        onClick: (...[$event]) => {
            __VLS_ctx.removeDraft(row);
        }
    };
    __VLS_107.slots.default;
    var __VLS_107;
}
var __VLS_79;
var __VLS_43;
var __VLS_11;
var __VLS_7;
const __VLS_112 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    label: "薪资配置",
    name: "profiles",
}));
const __VLS_114 = __VLS_113({
    label: "薪资配置",
    name: "profiles",
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
__VLS_115.slots.default;
const __VLS_116 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
    shadow: "never",
}));
const __VLS_118 = __VLS_117({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_117));
__VLS_119.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "title" },
});
(__VLS_ctx.activeStoreName);
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_120 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_122 = __VLS_121({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
let __VLS_124;
let __VLS_125;
let __VLS_126;
const __VLS_127 = {
    onClick: (__VLS_ctx.loadProfiles)
};
__VLS_123.slots.default;
var __VLS_123;
const __VLS_128 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    data: (__VLS_ctx.profiles),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_130 = __VLS_129({
    data: (__VLS_ctx.profiles),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_131.slots.default;
const __VLS_132 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    prop: "userName",
    label: "员工",
    width: "140",
}));
const __VLS_134 = __VLS_133({
    prop: "userName",
    label: "员工",
    width: "140",
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
const __VLS_136 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
    label: "月底薪",
    width: "120",
}));
const __VLS_138 = __VLS_137({
    label: "月底薪",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_137));
__VLS_139.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_139.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.baseMonthly.toFixed(2));
}
var __VLS_139;
const __VLS_140 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    label: "加班时薪",
    width: "120",
}));
const __VLS_142 = __VLS_141({
    label: "加班时薪",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
__VLS_143.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_143.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.overtimeHourRate.toFixed(2));
}
var __VLS_143;
const __VLS_144 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    label: "满勤奖",
    width: "140",
}));
const __VLS_146 = __VLS_145({
    label: "满勤奖",
    width: "140",
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
__VLS_147.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_147.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.attendanceBonusAmount.toFixed(2));
    (row.requiredAttendanceDays);
}
var __VLS_147;
const __VLS_148 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    prop: "remark",
    label: "备注",
    minWidth: "160",
}));
const __VLS_150 = __VLS_149({
    prop: "remark",
    label: "备注",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
const __VLS_152 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
    label: "操作",
    width: "120",
    fixed: "right",
}));
const __VLS_154 = __VLS_153({
    label: "操作",
    width: "120",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_153));
__VLS_155.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_155.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_156 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
        ...{ 'onClick': {} },
        size: "small",
    }));
    const __VLS_158 = __VLS_157({
        ...{ 'onClick': {} },
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_157));
    let __VLS_160;
    let __VLS_161;
    let __VLS_162;
    const __VLS_163 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openProfile(row);
        }
    };
    __VLS_159.slots.default;
    var __VLS_159;
}
var __VLS_155;
var __VLS_131;
if (__VLS_ctx.profiles.length === 0) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "empty" },
    });
}
var __VLS_119;
var __VLS_115;
var __VLS_3;
const __VLS_164 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    modelValue: (__VLS_ctx.detailOpen),
    title: (__VLS_ctx.detailTitle),
    width: "980px",
}));
const __VLS_166 = __VLS_165({
    modelValue: (__VLS_ctx.detailOpen),
    title: (__VLS_ctx.detailTitle),
    width: "980px",
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_167.slots.default;
if (__VLS_ctx.detail) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "detail" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "metric-row" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    const __VLS_168 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
        type: (__VLS_ctx.statusTag(__VLS_ctx.detail.period.status)),
    }));
    const __VLS_170 = __VLS_169({
        type: (__VLS_ctx.statusTag(__VLS_ctx.detail.period.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_169));
    __VLS_171.slots.default;
    (__VLS_ctx.statusLabel(__VLS_ctx.detail.period.status));
    var __VLS_171;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.detail.period.totalAmount.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    (__VLS_ctx.detail.period.itemCount);
    const __VLS_172 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
        data: (__VLS_ctx.detail.items),
        stripe: true,
        size: "small",
    }));
    const __VLS_174 = __VLS_173({
        data: (__VLS_ctx.detail.items),
        stripe: true,
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_173));
    __VLS_175.slots.default;
    const __VLS_176 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
        prop: "employeeNo",
        label: "工号",
        width: "70",
    }));
    const __VLS_178 = __VLS_177({
        prop: "employeeNo",
        label: "工号",
        width: "70",
    }, ...__VLS_functionalComponentArgsRest(__VLS_177));
    const __VLS_180 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
        prop: "userName",
        label: "员工",
        width: "100",
    }));
    const __VLS_182 = __VLS_181({
        prop: "userName",
        label: "员工",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_181));
    const __VLS_184 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
        label: "底薪",
        width: "90",
    }));
    const __VLS_186 = __VLS_185({
        label: "底薪",
        width: "90",
    }, ...__VLS_functionalComponentArgsRest(__VLS_185));
    __VLS_187.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_187.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.baseSalary.toFixed(2));
    }
    var __VLS_187;
    const __VLS_188 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_189 = __VLS_asFunctionalComponent(__VLS_188, new __VLS_188({
        label: "提成",
        width: "100",
    }));
    const __VLS_190 = __VLS_189({
        label: "提成",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_189));
    __VLS_191.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_191.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.commissionTotal.toFixed(2));
    }
    var __VLS_191;
    const __VLS_192 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_193 = __VLS_asFunctionalComponent(__VLS_192, new __VLS_192({
        label: "加班",
        width: "120",
    }));
    const __VLS_194 = __VLS_193({
        label: "加班",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_193));
    __VLS_195.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_195.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.overtimeHours);
        (row.overtimeAmount.toFixed(2));
    }
    var __VLS_195;
    const __VLS_196 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_197 = __VLS_asFunctionalComponent(__VLS_196, new __VLS_196({
        label: "满勤",
        width: "90",
    }));
    const __VLS_198 = __VLS_197({
        label: "满勤",
        width: "90",
    }, ...__VLS_functionalComponentArgsRest(__VLS_197));
    __VLS_199.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_199.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.attendanceBonus.toFixed(2));
    }
    var __VLS_199;
    const __VLS_200 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_201 = __VLS_asFunctionalComponent(__VLS_200, new __VLS_200({
        label: "调整",
        width: "90",
    }));
    const __VLS_202 = __VLS_201({
        label: "调整",
        width: "90",
    }, ...__VLS_functionalComponentArgsRest(__VLS_201));
    __VLS_203.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_203.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: ({ neg: row.adjustmentTotal < 0 }) },
        });
        (row.adjustmentTotal.toFixed(2));
    }
    var __VLS_203;
    const __VLS_204 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_205 = __VLS_asFunctionalComponent(__VLS_204, new __VLS_204({
        label: "小费",
        width: "80",
    }));
    const __VLS_206 = __VLS_205({
        label: "小费",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_205));
    __VLS_207.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_207.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.tipsTotal.toFixed(2));
    }
    var __VLS_207;
    const __VLS_208 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_209 = __VLS_asFunctionalComponent(__VLS_208, new __VLS_208({
        label: "净额",
        width: "120",
    }));
    const __VLS_210 = __VLS_209({
        label: "净额",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_209));
    __VLS_211.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_211.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
            ...{ style: {} },
        });
        (row.netTotal.toFixed(2));
    }
    var __VLS_211;
    const __VLS_212 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_213 = __VLS_asFunctionalComponent(__VLS_212, new __VLS_212({
        label: "排班/请假",
        width: "100",
    }));
    const __VLS_214 = __VLS_213({
        label: "排班/请假",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_213));
    __VLS_215.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_215.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.scheduledDays);
        (row.leaveDays);
    }
    var __VLS_215;
    const __VLS_216 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_217 = __VLS_asFunctionalComponent(__VLS_216, new __VLS_216({
        label: "操作",
        width: "160",
        fixed: "right",
    }));
    const __VLS_218 = __VLS_217({
        label: "操作",
        width: "160",
        fixed: "right",
    }, ...__VLS_functionalComponentArgsRest(__VLS_217));
    __VLS_219.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_219.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        const __VLS_220 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_221 = __VLS_asFunctionalComponent(__VLS_220, new __VLS_220({
            ...{ 'onClick': {} },
            size: "small",
            disabled: (__VLS_ctx.detail.period.status !== 'Draft'),
        }));
        const __VLS_222 = __VLS_221({
            ...{ 'onClick': {} },
            size: "small",
            disabled: (__VLS_ctx.detail.period.status !== 'Draft'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_221));
        let __VLS_224;
        let __VLS_225;
        let __VLS_226;
        const __VLS_227 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.detail))
                    return;
                __VLS_ctx.openEditItem(row);
            }
        };
        __VLS_223.slots.default;
        var __VLS_223;
        const __VLS_228 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_229 = __VLS_asFunctionalComponent(__VLS_228, new __VLS_228({
            ...{ 'onClick': {} },
            size: "small",
            disabled: (__VLS_ctx.detail.period.status !== 'Draft'),
        }));
        const __VLS_230 = __VLS_229({
            ...{ 'onClick': {} },
            size: "small",
            disabled: (__VLS_ctx.detail.period.status !== 'Draft'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_229));
        let __VLS_232;
        let __VLS_233;
        let __VLS_234;
        const __VLS_235 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.detail))
                    return;
                __VLS_ctx.openAddAdj(row);
            }
        };
        __VLS_231.slots.default;
        var __VLS_231;
    }
    var __VLS_219;
    var __VLS_175;
    if (__VLS_ctx.expandedItem) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "adj-section" },
        });
        __VLS_asFunctionalElement(__VLS_intrinsicElements.h4, __VLS_intrinsicElements.h4)({});
        (__VLS_ctx.expandedItem.userName);
        const __VLS_236 = {}.ElTable;
        /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
        // @ts-ignore
        const __VLS_237 = __VLS_asFunctionalComponent(__VLS_236, new __VLS_236({
            data: (__VLS_ctx.expandedItem.adjustments),
            size: "small",
            stripe: true,
        }));
        const __VLS_238 = __VLS_237({
            data: (__VLS_ctx.expandedItem.adjustments),
            size: "small",
            stripe: true,
        }, ...__VLS_functionalComponentArgsRest(__VLS_237));
        __VLS_239.slots.default;
        const __VLS_240 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_241 = __VLS_asFunctionalComponent(__VLS_240, new __VLS_240({
            prop: "kind",
            label: "类型",
            width: "80",
        }));
        const __VLS_242 = __VLS_241({
            prop: "kind",
            label: "类型",
            width: "80",
        }, ...__VLS_functionalComponentArgsRest(__VLS_241));
        __VLS_243.slots.default;
        {
            const { default: __VLS_thisSlot } = __VLS_243.slots;
            const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
            (row.kind === 'Bonus' ? '奖金' : '扣款');
        }
        var __VLS_243;
        const __VLS_244 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_245 = __VLS_asFunctionalComponent(__VLS_244, new __VLS_244({
            label: "金额",
            width: "100",
        }));
        const __VLS_246 = __VLS_245({
            label: "金额",
            width: "100",
        }, ...__VLS_functionalComponentArgsRest(__VLS_245));
        __VLS_247.slots.default;
        {
            const { default: __VLS_thisSlot } = __VLS_247.slots;
            const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
            __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                ...{ class: ({ neg: row.kind === 'Deduction' }) },
            });
            (row.kind === 'Bonus' ? '+' : '-');
            (row.amount.toFixed(2));
        }
        var __VLS_247;
        const __VLS_248 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_249 = __VLS_asFunctionalComponent(__VLS_248, new __VLS_248({
            prop: "reason",
            label: "原因",
            minWidth: "160",
        }));
        const __VLS_250 = __VLS_249({
            prop: "reason",
            label: "原因",
            minWidth: "160",
        }, ...__VLS_functionalComponentArgsRest(__VLS_249));
        const __VLS_252 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_253 = __VLS_asFunctionalComponent(__VLS_252, new __VLS_252({
            prop: "operatorName",
            label: "操作人",
            width: "100",
        }));
        const __VLS_254 = __VLS_253({
            prop: "operatorName",
            label: "操作人",
            width: "100",
        }, ...__VLS_functionalComponentArgsRest(__VLS_253));
        const __VLS_256 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_257 = __VLS_asFunctionalComponent(__VLS_256, new __VLS_256({
            prop: "createdAt",
            label: "时间",
            width: "160",
        }));
        const __VLS_258 = __VLS_257({
            prop: "createdAt",
            label: "时间",
            width: "160",
        }, ...__VLS_functionalComponentArgsRest(__VLS_257));
        const __VLS_260 = {}.ElTableColumn;
        /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
        // @ts-ignore
        const __VLS_261 = __VLS_asFunctionalComponent(__VLS_260, new __VLS_260({
            label: "操作",
            width: "80",
            fixed: "right",
        }));
        const __VLS_262 = __VLS_261({
            label: "操作",
            width: "80",
            fixed: "right",
        }, ...__VLS_functionalComponentArgsRest(__VLS_261));
        __VLS_263.slots.default;
        {
            const { default: __VLS_thisSlot } = __VLS_263.slots;
            const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
            const __VLS_264 = {}.ElButton;
            /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
            // @ts-ignore
            const __VLS_265 = __VLS_asFunctionalComponent(__VLS_264, new __VLS_264({
                ...{ 'onClick': {} },
                size: "small",
                type: "danger",
                disabled: (__VLS_ctx.detail.period.status !== 'Draft'),
            }));
            const __VLS_266 = __VLS_265({
                ...{ 'onClick': {} },
                size: "small",
                type: "danger",
                disabled: (__VLS_ctx.detail.period.status !== 'Draft'),
            }, ...__VLS_functionalComponentArgsRest(__VLS_265));
            let __VLS_268;
            let __VLS_269;
            let __VLS_270;
            const __VLS_271 = {
                onClick: (...[$event]) => {
                    if (!(__VLS_ctx.detail))
                        return;
                    if (!(__VLS_ctx.expandedItem))
                        return;
                    __VLS_ctx.removeAdj(__VLS_ctx.expandedItem.id, row.id);
                }
            };
            __VLS_267.slots.default;
            var __VLS_267;
        }
        var __VLS_263;
        var __VLS_239;
    }
}
var __VLS_167;
const __VLS_272 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_273 = __VLS_asFunctionalComponent(__VLS_272, new __VLS_272({
    modelValue: (__VLS_ctx.editItemOpen),
    title: "编辑工资项",
    width: "420px",
}));
const __VLS_274 = __VLS_273({
    modelValue: (__VLS_ctx.editItemOpen),
    title: "编辑工资项",
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_273));
__VLS_275.slots.default;
if (__VLS_ctx.editingItem) {
    const __VLS_276 = {}.ElForm;
    /** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
    // @ts-ignore
    const __VLS_277 = __VLS_asFunctionalComponent(__VLS_276, new __VLS_276({
        model: (__VLS_ctx.editForm),
        labelWidth: "120px",
    }));
    const __VLS_278 = __VLS_277({
        model: (__VLS_ctx.editForm),
        labelWidth: "120px",
    }, ...__VLS_functionalComponentArgsRest(__VLS_277));
    __VLS_279.slots.default;
    const __VLS_280 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_281 = __VLS_asFunctionalComponent(__VLS_280, new __VLS_280({
        label: "员工",
    }));
    const __VLS_282 = __VLS_281({
        label: "员工",
    }, ...__VLS_functionalComponentArgsRest(__VLS_281));
    __VLS_283.slots.default;
    (__VLS_ctx.editingItem.userName);
    var __VLS_283;
    const __VLS_284 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_285 = __VLS_asFunctionalComponent(__VLS_284, new __VLS_284({
        label: "加班小时数",
    }));
    const __VLS_286 = __VLS_285({
        label: "加班小时数",
    }, ...__VLS_functionalComponentArgsRest(__VLS_285));
    __VLS_287.slots.default;
    const __VLS_288 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_289 = __VLS_asFunctionalComponent(__VLS_288, new __VLS_288({
        modelValue: (__VLS_ctx.editForm.overtimeHours),
        min: (0),
        precision: (2),
    }));
    const __VLS_290 = __VLS_289({
        modelValue: (__VLS_ctx.editForm.overtimeHours),
        min: (0),
        precision: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_289));
    var __VLS_287;
    const __VLS_292 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_293 = __VLS_asFunctionalComponent(__VLS_292, new __VLS_292({
        label: "满勤奖（覆盖）",
    }));
    const __VLS_294 = __VLS_293({
        label: "满勤奖（覆盖）",
    }, ...__VLS_functionalComponentArgsRest(__VLS_293));
    __VLS_295.slots.default;
    const __VLS_296 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_297 = __VLS_asFunctionalComponent(__VLS_296, new __VLS_296({
        modelValue: (__VLS_ctx.editForm.attendanceBonusOverride),
        min: (-1),
        precision: (2),
    }));
    const __VLS_298 = __VLS_297({
        modelValue: (__VLS_ctx.editForm.attendanceBonusOverride),
        min: (-1),
        precision: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_297));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "hint" },
    });
    var __VLS_295;
    const __VLS_300 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_301 = __VLS_asFunctionalComponent(__VLS_300, new __VLS_300({
        label: "备注",
    }));
    const __VLS_302 = __VLS_301({
        label: "备注",
    }, ...__VLS_functionalComponentArgsRest(__VLS_301));
    __VLS_303.slots.default;
    const __VLS_304 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_305 = __VLS_asFunctionalComponent(__VLS_304, new __VLS_304({
        modelValue: (__VLS_ctx.editForm.remark),
        type: "textarea",
        rows: (2),
        maxlength: "500",
    }));
    const __VLS_306 = __VLS_305({
        modelValue: (__VLS_ctx.editForm.remark),
        type: "textarea",
        rows: (2),
        maxlength: "500",
    }, ...__VLS_functionalComponentArgsRest(__VLS_305));
    var __VLS_303;
    var __VLS_279;
}
{
    const { footer: __VLS_thisSlot } = __VLS_275.slots;
    const __VLS_308 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_309 = __VLS_asFunctionalComponent(__VLS_308, new __VLS_308({
        ...{ 'onClick': {} },
    }));
    const __VLS_310 = __VLS_309({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_309));
    let __VLS_312;
    let __VLS_313;
    let __VLS_314;
    const __VLS_315 = {
        onClick: (...[$event]) => {
            __VLS_ctx.editItemOpen = false;
        }
    };
    __VLS_311.slots.default;
    var __VLS_311;
    const __VLS_316 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_317 = __VLS_asFunctionalComponent(__VLS_316, new __VLS_316({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_318 = __VLS_317({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_317));
    let __VLS_320;
    let __VLS_321;
    let __VLS_322;
    const __VLS_323 = {
        onClick: (__VLS_ctx.saveItem)
    };
    __VLS_319.slots.default;
    var __VLS_319;
}
var __VLS_275;
const __VLS_324 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_325 = __VLS_asFunctionalComponent(__VLS_324, new __VLS_324({
    modelValue: (__VLS_ctx.addAdjOpen),
    title: "新增奖金/扣款",
    width: "420px",
}));
const __VLS_326 = __VLS_325({
    modelValue: (__VLS_ctx.addAdjOpen),
    title: "新增奖金/扣款",
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_325));
__VLS_327.slots.default;
if (__VLS_ctx.adjTarget) {
    const __VLS_328 = {}.ElForm;
    /** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
    // @ts-ignore
    const __VLS_329 = __VLS_asFunctionalComponent(__VLS_328, new __VLS_328({
        model: (__VLS_ctx.adjForm),
        labelWidth: "100px",
    }));
    const __VLS_330 = __VLS_329({
        model: (__VLS_ctx.adjForm),
        labelWidth: "100px",
    }, ...__VLS_functionalComponentArgsRest(__VLS_329));
    __VLS_331.slots.default;
    const __VLS_332 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_333 = __VLS_asFunctionalComponent(__VLS_332, new __VLS_332({
        label: "员工",
    }));
    const __VLS_334 = __VLS_333({
        label: "员工",
    }, ...__VLS_functionalComponentArgsRest(__VLS_333));
    __VLS_335.slots.default;
    (__VLS_ctx.adjTarget.userName);
    var __VLS_335;
    const __VLS_336 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_337 = __VLS_asFunctionalComponent(__VLS_336, new __VLS_336({
        label: "类型",
        required: true,
    }));
    const __VLS_338 = __VLS_337({
        label: "类型",
        required: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_337));
    __VLS_339.slots.default;
    const __VLS_340 = {}.ElRadioGroup;
    /** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
    // @ts-ignore
    const __VLS_341 = __VLS_asFunctionalComponent(__VLS_340, new __VLS_340({
        modelValue: (__VLS_ctx.adjForm.kind),
    }));
    const __VLS_342 = __VLS_341({
        modelValue: (__VLS_ctx.adjForm.kind),
    }, ...__VLS_functionalComponentArgsRest(__VLS_341));
    __VLS_343.slots.default;
    const __VLS_344 = {}.ElRadio;
    /** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
    // @ts-ignore
    const __VLS_345 = __VLS_asFunctionalComponent(__VLS_344, new __VLS_344({
        value: "Bonus",
    }));
    const __VLS_346 = __VLS_345({
        value: "Bonus",
    }, ...__VLS_functionalComponentArgsRest(__VLS_345));
    __VLS_347.slots.default;
    var __VLS_347;
    const __VLS_348 = {}.ElRadio;
    /** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
    // @ts-ignore
    const __VLS_349 = __VLS_asFunctionalComponent(__VLS_348, new __VLS_348({
        value: "Deduction",
    }));
    const __VLS_350 = __VLS_349({
        value: "Deduction",
    }, ...__VLS_functionalComponentArgsRest(__VLS_349));
    __VLS_351.slots.default;
    var __VLS_351;
    var __VLS_343;
    var __VLS_339;
    const __VLS_352 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_353 = __VLS_asFunctionalComponent(__VLS_352, new __VLS_352({
        label: "金额",
        required: true,
    }));
    const __VLS_354 = __VLS_353({
        label: "金额",
        required: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_353));
    __VLS_355.slots.default;
    const __VLS_356 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_357 = __VLS_asFunctionalComponent(__VLS_356, new __VLS_356({
        modelValue: (__VLS_ctx.adjForm.amount),
        min: (0.01),
        precision: (2),
    }));
    const __VLS_358 = __VLS_357({
        modelValue: (__VLS_ctx.adjForm.amount),
        min: (0.01),
        precision: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_357));
    var __VLS_355;
    const __VLS_360 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_361 = __VLS_asFunctionalComponent(__VLS_360, new __VLS_360({
        label: "原因",
        required: true,
    }));
    const __VLS_362 = __VLS_361({
        label: "原因",
        required: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_361));
    __VLS_363.slots.default;
    const __VLS_364 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_365 = __VLS_asFunctionalComponent(__VLS_364, new __VLS_364({
        modelValue: (__VLS_ctx.adjForm.reason),
        maxlength: "200",
        placeholder: "如：迟到 3 次扣款 / 推荐新员工奖金",
    }));
    const __VLS_366 = __VLS_365({
        modelValue: (__VLS_ctx.adjForm.reason),
        maxlength: "200",
        placeholder: "如：迟到 3 次扣款 / 推荐新员工奖金",
    }, ...__VLS_functionalComponentArgsRest(__VLS_365));
    var __VLS_363;
    var __VLS_331;
}
{
    const { footer: __VLS_thisSlot } = __VLS_327.slots;
    const __VLS_368 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_369 = __VLS_asFunctionalComponent(__VLS_368, new __VLS_368({
        ...{ 'onClick': {} },
    }));
    const __VLS_370 = __VLS_369({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_369));
    let __VLS_372;
    let __VLS_373;
    let __VLS_374;
    const __VLS_375 = {
        onClick: (...[$event]) => {
            __VLS_ctx.addAdjOpen = false;
        }
    };
    __VLS_371.slots.default;
    var __VLS_371;
    const __VLS_376 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_377 = __VLS_asFunctionalComponent(__VLS_376, new __VLS_376({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_378 = __VLS_377({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_377));
    let __VLS_380;
    let __VLS_381;
    let __VLS_382;
    const __VLS_383 = {
        onClick: (__VLS_ctx.saveAdj)
    };
    __VLS_379.slots.default;
    var __VLS_379;
}
var __VLS_327;
const __VLS_384 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_385 = __VLS_asFunctionalComponent(__VLS_384, new __VLS_384({
    modelValue: (__VLS_ctx.profileOpen),
    title: "薪资配置",
    width: "460px",
}));
const __VLS_386 = __VLS_385({
    modelValue: (__VLS_ctx.profileOpen),
    title: "薪资配置",
    width: "460px",
}, ...__VLS_functionalComponentArgsRest(__VLS_385));
__VLS_387.slots.default;
if (__VLS_ctx.editingProfile) {
    const __VLS_388 = {}.ElForm;
    /** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
    // @ts-ignore
    const __VLS_389 = __VLS_asFunctionalComponent(__VLS_388, new __VLS_388({
        model: (__VLS_ctx.profileForm),
        labelWidth: "110px",
    }));
    const __VLS_390 = __VLS_389({
        model: (__VLS_ctx.profileForm),
        labelWidth: "110px",
    }, ...__VLS_functionalComponentArgsRest(__VLS_389));
    __VLS_391.slots.default;
    const __VLS_392 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_393 = __VLS_asFunctionalComponent(__VLS_392, new __VLS_392({
        label: "员工",
    }));
    const __VLS_394 = __VLS_393({
        label: "员工",
    }, ...__VLS_functionalComponentArgsRest(__VLS_393));
    __VLS_395.slots.default;
    (__VLS_ctx.editingProfile.userName);
    var __VLS_395;
    const __VLS_396 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_397 = __VLS_asFunctionalComponent(__VLS_396, new __VLS_396({
        label: "月底薪",
    }));
    const __VLS_398 = __VLS_397({
        label: "月底薪",
    }, ...__VLS_functionalComponentArgsRest(__VLS_397));
    __VLS_399.slots.default;
    const __VLS_400 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_401 = __VLS_asFunctionalComponent(__VLS_400, new __VLS_400({
        modelValue: (__VLS_ctx.profileForm.baseMonthly),
        min: (0),
        precision: (2),
    }));
    const __VLS_402 = __VLS_401({
        modelValue: (__VLS_ctx.profileForm.baseMonthly),
        min: (0),
        precision: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_401));
    var __VLS_399;
    const __VLS_404 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_405 = __VLS_asFunctionalComponent(__VLS_404, new __VLS_404({
        label: "加班时薪",
    }));
    const __VLS_406 = __VLS_405({
        label: "加班时薪",
    }, ...__VLS_functionalComponentArgsRest(__VLS_405));
    __VLS_407.slots.default;
    const __VLS_408 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_409 = __VLS_asFunctionalComponent(__VLS_408, new __VLS_408({
        modelValue: (__VLS_ctx.profileForm.overtimeHourRate),
        min: (0),
        precision: (2),
    }));
    const __VLS_410 = __VLS_409({
        modelValue: (__VLS_ctx.profileForm.overtimeHourRate),
        min: (0),
        precision: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_409));
    var __VLS_407;
    const __VLS_412 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_413 = __VLS_asFunctionalComponent(__VLS_412, new __VLS_412({
        label: "满勤奖额度",
    }));
    const __VLS_414 = __VLS_413({
        label: "满勤奖额度",
    }, ...__VLS_functionalComponentArgsRest(__VLS_413));
    __VLS_415.slots.default;
    const __VLS_416 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_417 = __VLS_asFunctionalComponent(__VLS_416, new __VLS_416({
        modelValue: (__VLS_ctx.profileForm.attendanceBonusAmount),
        min: (0),
        precision: (2),
    }));
    const __VLS_418 = __VLS_417({
        modelValue: (__VLS_ctx.profileForm.attendanceBonusAmount),
        min: (0),
        precision: (2),
    }, ...__VLS_functionalComponentArgsRest(__VLS_417));
    var __VLS_415;
    const __VLS_420 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_421 = __VLS_asFunctionalComponent(__VLS_420, new __VLS_420({
        label: "满勤所需天数",
    }));
    const __VLS_422 = __VLS_421({
        label: "满勤所需天数",
    }, ...__VLS_functionalComponentArgsRest(__VLS_421));
    __VLS_423.slots.default;
    const __VLS_424 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_425 = __VLS_asFunctionalComponent(__VLS_424, new __VLS_424({
        modelValue: (__VLS_ctx.profileForm.requiredAttendanceDays),
        min: (0),
    }));
    const __VLS_426 = __VLS_425({
        modelValue: (__VLS_ctx.profileForm.requiredAttendanceDays),
        min: (0),
    }, ...__VLS_functionalComponentArgsRest(__VLS_425));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "hint" },
    });
    var __VLS_423;
    const __VLS_428 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_429 = __VLS_asFunctionalComponent(__VLS_428, new __VLS_428({
        label: "备注",
    }));
    const __VLS_430 = __VLS_429({
        label: "备注",
    }, ...__VLS_functionalComponentArgsRest(__VLS_429));
    __VLS_431.slots.default;
    const __VLS_432 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_433 = __VLS_asFunctionalComponent(__VLS_432, new __VLS_432({
        modelValue: (__VLS_ctx.profileForm.remark),
        type: "textarea",
        rows: (2),
        maxlength: "500",
    }));
    const __VLS_434 = __VLS_433({
        modelValue: (__VLS_ctx.profileForm.remark),
        type: "textarea",
        rows: (2),
        maxlength: "500",
    }, ...__VLS_functionalComponentArgsRest(__VLS_433));
    var __VLS_431;
    var __VLS_391;
}
{
    const { footer: __VLS_thisSlot } = __VLS_387.slots;
    const __VLS_436 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_437 = __VLS_asFunctionalComponent(__VLS_436, new __VLS_436({
        ...{ 'onClick': {} },
    }));
    const __VLS_438 = __VLS_437({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_437));
    let __VLS_440;
    let __VLS_441;
    let __VLS_442;
    const __VLS_443 = {
        onClick: (...[$event]) => {
            __VLS_ctx.profileOpen = false;
        }
    };
    __VLS_439.slots.default;
    var __VLS_439;
    const __VLS_444 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_445 = __VLS_asFunctionalComponent(__VLS_444, new __VLS_444({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_446 = __VLS_445({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_445));
    let __VLS_448;
    let __VLS_449;
    let __VLS_450;
    const __VLS_451 = {
        onClick: (__VLS_ctx.saveProfile)
    };
    __VLS_447.slots.default;
    var __VLS_447;
}
var __VLS_387;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
/** @type {__VLS_StyleScopedClasses['empty']} */ ;
/** @type {__VLS_StyleScopedClasses['detail']} */ ;
/** @type {__VLS_StyleScopedClasses['metric-row']} */ ;
/** @type {__VLS_StyleScopedClasses['adj-section']} */ ;
/** @type {__VLS_StyleScopedClasses['hint']} */ ;
/** @type {__VLS_StyleScopedClasses['hint']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Plus: Plus,
            Refresh: Refresh,
            tab: tab,
            periods: periods,
            profiles: profiles,
            loading: loading,
            saving: saving,
            generating: generating,
            yearFilter: yearFilter,
            genMonth: genMonth,
            detailOpen: detailOpen,
            detail: detail,
            expandedItem: expandedItem,
            editItemOpen: editItemOpen,
            editingItem: editingItem,
            editForm: editForm,
            addAdjOpen: addAdjOpen,
            adjTarget: adjTarget,
            adjForm: adjForm,
            profileOpen: profileOpen,
            editingProfile: editingProfile,
            profileForm: profileForm,
            activeStoreName: activeStoreName,
            detailTitle: detailTitle,
            statusLabel: statusLabel,
            statusTag: statusTag,
            loadPeriods: loadPeriods,
            loadProfiles: loadProfiles,
            generate: generate,
            openDetail: openDetail,
            lockPeriod: lockPeriod,
            markPaid: markPaid,
            removeDraft: removeDraft,
            openEditItem: openEditItem,
            saveItem: saveItem,
            openAddAdj: openAddAdj,
            saveAdj: saveAdj,
            removeAdj: removeAdj,
            openProfile: openProfile,
            saveProfile: saveProfile,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
