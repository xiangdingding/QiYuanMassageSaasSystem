import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { inventoryApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const loading = ref(false);
const onlyLowStock = ref(false);
const formOpen = ref(false);
const saving = ref(false);
const movementOpen = ref(false);
const moving = ref(false);
const historyOpen = ref(false);
const movements = ref([]);
const historyItemName = ref('');
const form = reactive({
    code: '', name: '', unit: '',
    quantity: 0, minQuantity: 0, unitCost: 0,
    remark: ''
});
const movement = reactive({
    itemId: 0, itemName: '',
    kind: 'PurchaseIn', delta: 1, remark: ''
});
const movementTitle = computed(() => {
    switch (movement.kind) {
        case 'PurchaseIn': return '采购入库';
        case 'Consume': return '领用出库';
        case 'Adjust': return '盘点调整';
        case 'Discard': return '报损';
        default: return '出入库';
    }
});
const movementHint = computed(() => {
    if (movement.kind === 'Consume' || movement.kind === 'Discard')
        return '系统会自动取负数';
    if (movement.kind === 'Adjust')
        return '正数=加，负数=减';
    return '正数表示新增库存';
});
const activeStoreName = computed(() => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? '');
async function reload() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        rows.value = await inventoryApi.items(appStore.activeStoreId, onlyLowStock.value);
    }
    finally {
        loading.value = false;
    }
}
function openNew() {
    Object.assign(form, { code: '', name: '', unit: '', quantity: 0, minQuantity: 0, unitCost: 0, remark: '' });
    formOpen.value = true;
}
async function save() {
    if (!form.code.trim() || !form.name.trim()) {
        ElMessage.warning('编码和名称必填');
        return;
    }
    saving.value = true;
    try {
        await inventoryApi.createItem({
            storeId: appStore.activeStoreId,
            code: form.code.trim(), name: form.name.trim(), unit: form.unit || null,
            quantity: form.quantity, minQuantity: form.minQuantity,
            unitCost: form.unitCost || null, remark: form.remark || null
        });
        formOpen.value = false;
        ElMessage.success('已建档');
        await reload();
    }
    finally {
        saving.value = false;
    }
}
function openMovement(row, kind) {
    movement.itemId = row.id;
    movement.itemName = row.name;
    movement.kind = kind;
    movement.delta = 1;
    movement.remark = '';
    movementOpen.value = true;
}
async function submitMovement() {
    moving.value = true;
    try {
        await inventoryApi.move({
            itemId: movement.itemId, kind: movement.kind,
            delta: movement.delta, remark: movement.remark || null
        });
        movementOpen.value = false;
        ElMessage.success('已记录');
        await reload();
    }
    finally {
        moving.value = false;
    }
}
async function viewMovements(row) {
    historyItemName.value = row.name;
    movements.value = await inventoryApi.movements(row.id, 50);
    historyOpen.value = true;
}
watch(() => appStore.activeStoreId, () => reload());
onMounted(async () => {
    await appStore.loadStores();
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
(__VLS_ctx.activeStoreName);
const __VLS_4 = {}.ElCheckbox;
/** @type {[typeof __VLS_components.ElCheckbox, typeof __VLS_components.elCheckbox, typeof __VLS_components.ElCheckbox, typeof __VLS_components.elCheckbox, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.onlyLowStock),
}));
const __VLS_6 = __VLS_5({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.onlyLowStock),
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onChange: (__VLS_ctx.reload)
};
__VLS_7.slots.default;
var __VLS_7;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_12 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
}));
const __VLS_14 = __VLS_13({
    ...{ 'onClick': {} },
    type: "primary",
    icon: (__VLS_ctx.Plus),
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
let __VLS_16;
let __VLS_17;
let __VLS_18;
const __VLS_19 = {
    onClick: (__VLS_ctx.openNew)
};
__VLS_15.slots.default;
var __VLS_15;
const __VLS_20 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_22 = __VLS_21({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
let __VLS_24;
let __VLS_25;
let __VLS_26;
const __VLS_27 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_23.slots.default;
var __VLS_23;
const __VLS_28 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_30 = __VLS_29({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_31.slots.default;
const __VLS_32 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    prop: "code",
    label: "编码",
    width: "120",
}));
const __VLS_34 = __VLS_33({
    prop: "code",
    label: "编码",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
const __VLS_36 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    prop: "name",
    label: "名称",
    minWidth: "160",
}));
const __VLS_38 = __VLS_37({
    prop: "name",
    label: "名称",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    prop: "unit",
    label: "单位",
    width: "80",
}));
const __VLS_42 = __VLS_41({
    prop: "unit",
    label: "单位",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    label: "库存",
    width: "120",
}));
const __VLS_46 = __VLS_45({
    label: "库存",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_47.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: ({ low: row.lowStock }) },
        'aria-label': (`${row.name} 库存 ${row.quantity} ${row.unit || ''}${row.lowStock ? '，已低于预警' : ''}`),
    });
    (row.quantity);
    (row.unit || '');
    if (row.lowStock) {
        const __VLS_48 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
            type: "danger",
            size: "small",
            ...{ style: {} },
        }));
        const __VLS_50 = __VLS_49({
            type: "danger",
            size: "small",
            ...{ style: {} },
        }, ...__VLS_functionalComponentArgsRest(__VLS_49));
        __VLS_51.slots.default;
        var __VLS_51;
    }
}
var __VLS_47;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "minQuantity",
    label: "预警阈值",
    width: "100",
}));
const __VLS_54 = __VLS_53({
    prop: "minQuantity",
    label: "预警阈值",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "unitCost",
    label: "单价",
    width: "100",
}));
const __VLS_58 = __VLS_57({
    prop: "unitCost",
    label: "单价",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
__VLS_59.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_59.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.unitCost ?? '—');
}
var __VLS_59;
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "操作",
    width: "280",
    fixed: "right",
}));
const __VLS_62 = __VLS_61({
    label: "操作",
    width: "280",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_64 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        ...{ 'onClick': {} },
        size: "small",
    }));
    const __VLS_66 = __VLS_65({
        ...{ 'onClick': {} },
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    let __VLS_68;
    let __VLS_69;
    let __VLS_70;
    const __VLS_71 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openMovement(row, 'PurchaseIn');
        }
    };
    __VLS_67.slots.default;
    var __VLS_67;
    const __VLS_72 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
        ...{ 'onClick': {} },
        size: "small",
    }));
    const __VLS_74 = __VLS_73({
        ...{ 'onClick': {} },
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_73));
    let __VLS_76;
    let __VLS_77;
    let __VLS_78;
    const __VLS_79 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openMovement(row, 'Consume');
        }
    };
    __VLS_75.slots.default;
    var __VLS_75;
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
            __VLS_ctx.openMovement(row, 'Adjust');
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
        type: "info",
    }));
    const __VLS_90 = __VLS_89({
        ...{ 'onClick': {} },
        size: "small",
        type: "info",
    }, ...__VLS_functionalComponentArgsRest(__VLS_89));
    let __VLS_92;
    let __VLS_93;
    let __VLS_94;
    const __VLS_95 = {
        onClick: (...[$event]) => {
            __VLS_ctx.viewMovements(row);
        }
    };
    __VLS_91.slots.default;
    var __VLS_91;
}
var __VLS_63;
var __VLS_31;
var __VLS_3;
const __VLS_96 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    modelValue: (__VLS_ctx.formOpen),
    title: "物品建档",
    width: "460px",
}));
const __VLS_98 = __VLS_97({
    modelValue: (__VLS_ctx.formOpen),
    title: "物品建档",
    width: "460px",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
__VLS_99.slots.default;
const __VLS_100 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    model: (__VLS_ctx.form),
    labelWidth: "90px",
}));
const __VLS_102 = __VLS_101({
    model: (__VLS_ctx.form),
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_103.slots.default;
const __VLS_104 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
    label: "编码",
    required: true,
}));
const __VLS_106 = __VLS_105({
    label: "编码",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_105));
__VLS_107.slots.default;
const __VLS_108 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
    modelValue: (__VLS_ctx.form.code),
    maxlength: "64",
}));
const __VLS_110 = __VLS_109({
    modelValue: (__VLS_ctx.form.code),
    maxlength: "64",
}, ...__VLS_functionalComponentArgsRest(__VLS_109));
var __VLS_107;
const __VLS_112 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    label: "名称",
    required: true,
}));
const __VLS_114 = __VLS_113({
    label: "名称",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
__VLS_115.slots.default;
const __VLS_116 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
    modelValue: (__VLS_ctx.form.name),
    maxlength: "200",
}));
const __VLS_118 = __VLS_117({
    modelValue: (__VLS_ctx.form.name),
    maxlength: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_117));
var __VLS_115;
const __VLS_120 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    label: "单位",
}));
const __VLS_122 = __VLS_121({
    label: "单位",
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
__VLS_123.slots.default;
const __VLS_124 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
    modelValue: (__VLS_ctx.form.unit),
    maxlength: "16",
    placeholder: "瓶/包/个",
}));
const __VLS_126 = __VLS_125({
    modelValue: (__VLS_ctx.form.unit),
    maxlength: "16",
    placeholder: "瓶/包/个",
}, ...__VLS_functionalComponentArgsRest(__VLS_125));
var __VLS_123;
const __VLS_128 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    label: "初始库存",
}));
const __VLS_130 = __VLS_129({
    label: "初始库存",
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
__VLS_131.slots.default;
const __VLS_132 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
    modelValue: (__VLS_ctx.form.quantity),
    min: (0),
    precision: (3),
}));
const __VLS_134 = __VLS_133({
    modelValue: (__VLS_ctx.form.quantity),
    min: (0),
    precision: (3),
}, ...__VLS_functionalComponentArgsRest(__VLS_133));
var __VLS_131;
const __VLS_136 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
    label: "预警阈值",
}));
const __VLS_138 = __VLS_137({
    label: "预警阈值",
}, ...__VLS_functionalComponentArgsRest(__VLS_137));
__VLS_139.slots.default;
const __VLS_140 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    modelValue: (__VLS_ctx.form.minQuantity),
    min: (0),
    precision: (3),
}));
const __VLS_142 = __VLS_141({
    modelValue: (__VLS_ctx.form.minQuantity),
    min: (0),
    precision: (3),
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
var __VLS_139;
const __VLS_144 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
    label: "单价",
}));
const __VLS_146 = __VLS_145({
    label: "单价",
}, ...__VLS_functionalComponentArgsRest(__VLS_145));
__VLS_147.slots.default;
const __VLS_148 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
    modelValue: (__VLS_ctx.form.unitCost),
    min: (0),
    precision: (2),
}));
const __VLS_150 = __VLS_149({
    modelValue: (__VLS_ctx.form.unitCost),
    min: (0),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_149));
var __VLS_147;
const __VLS_152 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
    label: "备注",
}));
const __VLS_154 = __VLS_153({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_153));
__VLS_155.slots.default;
const __VLS_156 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_158 = __VLS_157({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
var __VLS_155;
var __VLS_103;
{
    const { footer: __VLS_thisSlot } = __VLS_99.slots;
    const __VLS_160 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
        ...{ 'onClick': {} },
    }));
    const __VLS_162 = __VLS_161({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_161));
    let __VLS_164;
    let __VLS_165;
    let __VLS_166;
    const __VLS_167 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_163.slots.default;
    var __VLS_163;
    const __VLS_168 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_170 = __VLS_169({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_169));
    let __VLS_172;
    let __VLS_173;
    let __VLS_174;
    const __VLS_175 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_171.slots.default;
    var __VLS_171;
}
var __VLS_99;
const __VLS_176 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
    modelValue: (__VLS_ctx.movementOpen),
    title: (__VLS_ctx.movementTitle),
    width: "420px",
}));
const __VLS_178 = __VLS_177({
    modelValue: (__VLS_ctx.movementOpen),
    title: (__VLS_ctx.movementTitle),
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_177));
__VLS_179.slots.default;
const __VLS_180 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
    model: (__VLS_ctx.movement),
    labelWidth: "90px",
}));
const __VLS_182 = __VLS_181({
    model: (__VLS_ctx.movement),
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_181));
__VLS_183.slots.default;
const __VLS_184 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
    label: "物品",
}));
const __VLS_186 = __VLS_185({
    label: "物品",
}, ...__VLS_functionalComponentArgsRest(__VLS_185));
__VLS_187.slots.default;
(__VLS_ctx.movement.itemName);
var __VLS_187;
const __VLS_188 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_189 = __VLS_asFunctionalComponent(__VLS_188, new __VLS_188({
    label: "数量",
    required: true,
}));
const __VLS_190 = __VLS_189({
    label: "数量",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_189));
__VLS_191.slots.default;
const __VLS_192 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_193 = __VLS_asFunctionalComponent(__VLS_192, new __VLS_192({
    modelValue: (__VLS_ctx.movement.delta),
    precision: (3),
}));
const __VLS_194 = __VLS_193({
    modelValue: (__VLS_ctx.movement.delta),
    precision: (3),
}, ...__VLS_functionalComponentArgsRest(__VLS_193));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ style: {} },
});
(__VLS_ctx.movementHint);
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
    modelValue: (__VLS_ctx.movement.remark),
    maxlength: "200",
}));
const __VLS_202 = __VLS_201({
    modelValue: (__VLS_ctx.movement.remark),
    maxlength: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_201));
var __VLS_199;
var __VLS_183;
{
    const { footer: __VLS_thisSlot } = __VLS_179.slots;
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
            __VLS_ctx.movementOpen = false;
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
        loading: (__VLS_ctx.moving),
    }));
    const __VLS_214 = __VLS_213({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.moving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_213));
    let __VLS_216;
    let __VLS_217;
    let __VLS_218;
    const __VLS_219 = {
        onClick: (__VLS_ctx.submitMovement)
    };
    __VLS_215.slots.default;
    var __VLS_215;
}
var __VLS_179;
const __VLS_220 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_221 = __VLS_asFunctionalComponent(__VLS_220, new __VLS_220({
    modelValue: (__VLS_ctx.historyOpen),
    title: (`${__VLS_ctx.historyItemName} 出入库流水`),
    width: "640px",
}));
const __VLS_222 = __VLS_221({
    modelValue: (__VLS_ctx.historyOpen),
    title: (`${__VLS_ctx.historyItemName} 出入库流水`),
    width: "640px",
}, ...__VLS_functionalComponentArgsRest(__VLS_221));
__VLS_223.slots.default;
const __VLS_224 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_225 = __VLS_asFunctionalComponent(__VLS_224, new __VLS_224({
    data: (__VLS_ctx.movements),
    stripe: true,
}));
const __VLS_226 = __VLS_225({
    data: (__VLS_ctx.movements),
    stripe: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_225));
__VLS_227.slots.default;
const __VLS_228 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_229 = __VLS_asFunctionalComponent(__VLS_228, new __VLS_228({
    prop: "kind",
    label: "类型",
    width: "100",
}));
const __VLS_230 = __VLS_229({
    prop: "kind",
    label: "类型",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_229));
const __VLS_232 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_233 = __VLS_asFunctionalComponent(__VLS_232, new __VLS_232({
    prop: "delta",
    label: "变化",
    width: "100",
}));
const __VLS_234 = __VLS_233({
    prop: "delta",
    label: "变化",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_233));
const __VLS_236 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_237 = __VLS_asFunctionalComponent(__VLS_236, new __VLS_236({
    prop: "quantityAfter",
    label: "结余",
    width: "100",
}));
const __VLS_238 = __VLS_237({
    prop: "quantityAfter",
    label: "结余",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_237));
const __VLS_240 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_241 = __VLS_asFunctionalComponent(__VLS_240, new __VLS_240({
    prop: "operatorName",
    label: "操作人",
    width: "120",
}));
const __VLS_242 = __VLS_241({
    prop: "operatorName",
    label: "操作人",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_241));
const __VLS_244 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_245 = __VLS_asFunctionalComponent(__VLS_244, new __VLS_244({
    prop: "remark",
    label: "备注",
    minWidth: "120",
}));
const __VLS_246 = __VLS_245({
    prop: "remark",
    label: "备注",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_245));
const __VLS_248 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_249 = __VLS_asFunctionalComponent(__VLS_248, new __VLS_248({
    prop: "createdAt",
    label: "时间",
    width: "180",
}));
const __VLS_250 = __VLS_249({
    prop: "createdAt",
    label: "时间",
    width: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_249));
var __VLS_227;
var __VLS_223;
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
            onlyLowStock: onlyLowStock,
            formOpen: formOpen,
            saving: saving,
            movementOpen: movementOpen,
            moving: moving,
            historyOpen: historyOpen,
            movements: movements,
            historyItemName: historyItemName,
            form: form,
            movement: movement,
            movementTitle: movementTitle,
            movementHint: movementHint,
            activeStoreName: activeStoreName,
            reload: reload,
            openNew: openNew,
            save: save,
            openMovement: openMovement,
            submitMovement: submitMovement,
            viewMovements: viewMovements,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
