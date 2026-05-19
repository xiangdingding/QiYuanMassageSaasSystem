import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { schedulesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const tab = ref('schedules');
const schedules = ref([]);
const leaves = ref([]);
const loading = ref(false);
const staffList = ref([]);
const range = ref(null);
const schedOpen = ref(false);
const saving = ref(false);
const sched = reactive({
    userId: 0, workDate: null,
    startTime: '09:00', endTime: '18:00', remark: ''
});
function formatDate(s) {
    return s ? new Date(s).toISOString().slice(0, 10) : '';
}
async function reload() {
    loading.value = true;
    try {
        if (tab.value === 'schedules') {
            if (!appStore.activeStoreId)
                return;
            const from = range.value?.[0]?.toISOString();
            const to = range.value?.[1]?.toISOString();
            schedules.value = await schedulesApi.list(appStore.activeStoreId, from, to);
        }
        else {
            leaves.value = await schedulesApi.leaves();
        }
    }
    finally {
        loading.value = false;
    }
}
function openSchedule() {
    Object.assign(sched, { userId: 0, workDate: new Date(), startTime: '09:00', endTime: '18:00', remark: '' });
    schedOpen.value = true;
}
async function saveSchedule() {
    if (!sched.userId || !sched.workDate) {
        ElMessage.warning('员工与日期必填');
        return;
    }
    saving.value = true;
    try {
        await schedulesApi.create({
            storeId: appStore.activeStoreId,
            userId: sched.userId,
            workDate: sched.workDate.toISOString(),
            startTime: sched.startTime,
            endTime: sched.endTime,
            remark: sched.remark || null
        });
        schedOpen.value = false;
        ElMessage.success('已保存');
        await reload();
    }
    finally {
        saving.value = false;
    }
}
async function removeSchedule(id) {
    await schedulesApi.remove(id);
    ElMessage.success('已删除');
    await reload();
}
async function approve(row, approve) {
    await schedulesApi.approveLeave(row.id, approve);
    ElMessage.success(approve ? '已同意' : '已驳回');
    await reload();
}
onMounted(async () => {
    const data = await staffApi.list({ page: 1, pageSize: 100 });
    staffList.value = data.items;
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_4 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_6 = __VLS_5({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_7.slots.default;
var __VLS_7;
const __VLS_12 = {}.ElTabs;
/** @type {[typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    ...{ 'onTabChange': {} },
    modelValue: (__VLS_ctx.tab),
    ...{ style: {} },
}));
const __VLS_14 = __VLS_13({
    ...{ 'onTabChange': {} },
    modelValue: (__VLS_ctx.tab),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
let __VLS_16;
let __VLS_17;
let __VLS_18;
const __VLS_19 = {
    onTabChange: (__VLS_ctx.reload)
};
__VLS_15.slots.default;
const __VLS_20 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    label: "排班",
    name: "schedules",
}));
const __VLS_22 = __VLS_21({
    label: "排班",
    name: "schedules",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
__VLS_23.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "sub-toolbar" },
});
const __VLS_24 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    modelValue: (__VLS_ctx.range),
    type: "daterange",
    rangeSeparator: "-",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
}));
const __VLS_26 = __VLS_25({
    modelValue: (__VLS_ctx.range),
    type: "daterange",
    rangeSeparator: "-",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
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
    onClick: (__VLS_ctx.openSchedule)
};
__VLS_31.slots.default;
var __VLS_31;
const __VLS_36 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    data: (__VLS_ctx.schedules),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_38 = __VLS_37({
    data: (__VLS_ctx.schedules),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_39.slots.default;
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    prop: "workDate",
    label: "日期",
    width: "120",
}));
const __VLS_42 = __VLS_41({
    prop: "workDate",
    label: "日期",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_43.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_43.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.formatDate(row.workDate));
}
var __VLS_43;
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    prop: "userName",
    label: "员工",
    width: "120",
}));
const __VLS_46 = __VLS_45({
    prop: "userName",
    label: "员工",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    label: "班次",
    width: "160",
}));
const __VLS_50 = __VLS_49({
    label: "班次",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
__VLS_51.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_51.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.startTime);
    (row.endTime);
}
var __VLS_51;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "remark",
    label: "备注",
    minWidth: "160",
}));
const __VLS_54 = __VLS_53({
    prop: "remark",
    label: "备注",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    label: "操作",
    width: "120",
}));
const __VLS_58 = __VLS_57({
    label: "操作",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
__VLS_59.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_59.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_60 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
    }));
    const __VLS_62 = __VLS_61({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
    }, ...__VLS_functionalComponentArgsRest(__VLS_61));
    let __VLS_64;
    let __VLS_65;
    let __VLS_66;
    const __VLS_67 = {
        onClick: (...[$event]) => {
            __VLS_ctx.removeSchedule(row.id);
        }
    };
    __VLS_63.slots.default;
    var __VLS_63;
}
var __VLS_59;
var __VLS_39;
var __VLS_23;
const __VLS_68 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    label: "请假审批",
    name: "leaves",
}));
const __VLS_70 = __VLS_69({
    label: "请假审批",
    name: "leaves",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
__VLS_71.slots.default;
const __VLS_72 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    data: (__VLS_ctx.leaves),
    stripe: true,
}));
const __VLS_74 = __VLS_73({
    data: (__VLS_ctx.leaves),
    stripe: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_75.slots.default;
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    prop: "userName",
    label: "员工",
    width: "120",
}));
const __VLS_78 = __VLS_77({
    prop: "userName",
    label: "员工",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    prop: "type",
    label: "类型",
    width: "100",
}));
const __VLS_82 = __VLS_81({
    prop: "type",
    label: "类型",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    label: "日期",
    width: "220",
}));
const __VLS_86 = __VLS_85({
    label: "日期",
    width: "220",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.formatDate(row.fromDate));
    (__VLS_ctx.formatDate(row.toDate));
}
var __VLS_87;
const __VLS_88 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    prop: "reason",
    label: "原因",
    minWidth: "180",
}));
const __VLS_90 = __VLS_89({
    prop: "reason",
    label: "原因",
    minWidth: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
const __VLS_92 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    prop: "status",
    label: "状态",
    width: "100",
}));
const __VLS_94 = __VLS_93({
    prop: "status",
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
const __VLS_96 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    prop: "approverName",
    label: "审批人",
    width: "100",
}));
const __VLS_98 = __VLS_97({
    prop: "approverName",
    label: "审批人",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
const __VLS_100 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    label: "操作",
    width: "180",
    fixed: "right",
}));
const __VLS_102 = __VLS_101({
    label: "操作",
    width: "180",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_103.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_103.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_104 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
        ...{ 'onClick': {} },
        size: "small",
        type: "success",
        disabled: (row.status !== 'Pending'),
    }));
    const __VLS_106 = __VLS_105({
        ...{ 'onClick': {} },
        size: "small",
        type: "success",
        disabled: (row.status !== 'Pending'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_105));
    let __VLS_108;
    let __VLS_109;
    let __VLS_110;
    const __VLS_111 = {
        onClick: (...[$event]) => {
            __VLS_ctx.approve(row, true);
        }
    };
    __VLS_107.slots.default;
    var __VLS_107;
    const __VLS_112 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Pending'),
    }));
    const __VLS_114 = __VLS_113({
        ...{ 'onClick': {} },
        size: "small",
        type: "danger",
        disabled: (row.status !== 'Pending'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_113));
    let __VLS_116;
    let __VLS_117;
    let __VLS_118;
    const __VLS_119 = {
        onClick: (...[$event]) => {
            __VLS_ctx.approve(row, false);
        }
    };
    __VLS_115.slots.default;
    var __VLS_115;
}
var __VLS_103;
var __VLS_75;
var __VLS_71;
var __VLS_15;
var __VLS_3;
const __VLS_120 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    modelValue: (__VLS_ctx.schedOpen),
    title: "新增排班",
    width: "420px",
}));
const __VLS_122 = __VLS_121({
    modelValue: (__VLS_ctx.schedOpen),
    title: "新增排班",
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
const __VLS_124 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    model: (__VLS_ctx.sched),
    labelWidth: "80px",
}));
const __VLS_126 = __VLS_125({
    model: (__VLS_ctx.sched),
    labelWidth: "80px",
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
__VLS_127.slots.default;
const __VLS_128 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    label: "员工",
    required: true,
}));
const __VLS_130 = __VLS_129({
    label: "员工",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_131.slots.default;
const __VLS_132 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    modelValue: (__VLS_ctx.sched.userId),
    filterable: true,
    placeholder: "选择员工",
    ...{ style: {} },
}));
const __VLS_134 = __VLS_133({
    modelValue: (__VLS_ctx.sched.userId),
    filterable: true,
    placeholder: "选择员工",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
__VLS_135.slots.default;
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.staffList))) {
    const __VLS_136 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
        key: (s.id),
        label: (`${s.realName || s.username}（${s.role}）`),
        value: (s.id),
    }));
    const __VLS_138 = __VLS_137({
        key: (s.id),
        label: (`${s.realName || s.username}（${s.role}）`),
        value: (s.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_137));
}
var __VLS_135;
var __VLS_131;
const __VLS_140 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    label: "日期",
    required: true,
}));
const __VLS_142 = __VLS_141({
    label: "日期",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
__VLS_143.slots.default;
const __VLS_144 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    modelValue: (__VLS_ctx.sched.workDate),
    type: "date",
    ...{ style: {} },
}));
const __VLS_146 = __VLS_145({
    modelValue: (__VLS_ctx.sched.workDate),
    type: "date",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
var __VLS_143;
const __VLS_148 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    label: "开始",
    required: true,
}));
const __VLS_150 = __VLS_149({
    label: "开始",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
__VLS_151.slots.default;
const __VLS_152 = {}.ElTimePicker;
/** @type {[typeof __VLS_components.ElTimePicker, typeof __VLS_components.elTimePicker, ]} */ ;
// @ts-ignore
const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
    modelValue: (__VLS_ctx.sched.startTime),
    format: "HH:mm",
    valueFormat: "HH:mm",
    ...{ style: {} },
}));
const __VLS_154 = __VLS_153({
    modelValue: (__VLS_ctx.sched.startTime),
    format: "HH:mm",
    valueFormat: "HH:mm",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_153));
var __VLS_151;
const __VLS_156 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
    label: "结束",
    required: true,
}));
const __VLS_158 = __VLS_157({
    label: "结束",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
__VLS_159.slots.default;
const __VLS_160 = {}.ElTimePicker;
/** @type {[typeof __VLS_components.ElTimePicker, typeof __VLS_components.elTimePicker, ]} */ ;
// @ts-ignore
const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
    modelValue: (__VLS_ctx.sched.endTime),
    format: "HH:mm",
    valueFormat: "HH:mm",
    ...{ style: {} },
}));
const __VLS_162 = __VLS_161({
    modelValue: (__VLS_ctx.sched.endTime),
    format: "HH:mm",
    valueFormat: "HH:mm",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_161));
var __VLS_159;
const __VLS_164 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    label: "备注",
}));
const __VLS_166 = __VLS_165({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_167.slots.default;
const __VLS_168 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
    modelValue: (__VLS_ctx.sched.remark),
    maxlength: "200",
}));
const __VLS_170 = __VLS_169({
    modelValue: (__VLS_ctx.sched.remark),
    maxlength: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_169));
var __VLS_167;
var __VLS_127;
{
    const { footer: __VLS_thisSlot } = __VLS_123.slots;
    const __VLS_172 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
        ...{ 'onClick': {} },
    }));
    const __VLS_174 = __VLS_173({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_173));
    let __VLS_176;
    let __VLS_177;
    let __VLS_178;
    const __VLS_179 = {
        onClick: (...[$event]) => {
            __VLS_ctx.schedOpen = false;
        }
    };
    __VLS_175.slots.default;
    var __VLS_175;
    const __VLS_180 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_182 = __VLS_181({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_181));
    let __VLS_184;
    let __VLS_185;
    let __VLS_186;
    const __VLS_187 = {
        onClick: (__VLS_ctx.saveSchedule)
    };
    __VLS_183.slots.default;
    var __VLS_183;
}
var __VLS_123;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
/** @type {__VLS_StyleScopedClasses['sub-toolbar']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Plus: Plus,
            Refresh: Refresh,
            tab: tab,
            schedules: schedules,
            leaves: leaves,
            loading: loading,
            staffList: staffList,
            range: range,
            schedOpen: schedOpen,
            saving: saving,
            sched: sched,
            formatDate: formatDate,
            reload: reload,
            openSchedule: openSchedule,
            saveSchedule: saveSchedule,
            removeSchedule: removeSchedule,
            approve: approve,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
