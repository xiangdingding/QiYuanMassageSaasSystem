import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { storesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const loading = ref(false);
const saving = ref(false);
const formOpen = ref(false);
const formMode = ref('create');
const editingId = ref(null);
const formRef = ref();
const form = reactive({
    name: '',
    address: '',
    phone: '',
    parentStoreId: null,
    isActive: true
});
const rules = {
    name: [{ required: true, message: '请输入名称', trigger: 'blur' }]
};
const headquarters = computed(() => rows.value.filter((s) => s.isHeadquarters));
async function reload() {
    loading.value = true;
    try {
        rows.value = await storesApi.list();
    }
    finally {
        loading.value = false;
    }
}
function openCreate() {
    formMode.value = 'create';
    editingId.value = null;
    Object.assign(form, {
        name: '', address: '', phone: '',
        parentStoreId: headquarters.value[0]?.id ?? null,
        isActive: true
    });
    formOpen.value = true;
}
function openEdit(row) {
    formMode.value = 'edit';
    editingId.value = row.id;
    Object.assign(form, {
        name: row.name,
        address: row.address ?? '',
        phone: row.phone ?? '',
        parentStoreId: row.parentStoreId ?? null,
        isActive: row.isActive
    });
    formOpen.value = true;
}
async function save() {
    if (!formRef.value)
        return;
    const ok = await formRef.value.validate().catch(() => false);
    if (!ok)
        return;
    saving.value = true;
    try {
        if (formMode.value === 'create') {
            await storesApi.create({
                name: form.name,
                address: form.address || null,
                phone: form.phone || null,
                parentStoreId: form.parentStoreId
            });
        }
        else if (editingId.value != null) {
            await storesApi.update(editingId.value, {
                name: form.name,
                address: form.address || null,
                phone: form.phone || null,
                isActive: form.isActive
            });
        }
        ElMessage.success('已保存');
        formOpen.value = false;
        await appStore.loadStores(true);
        reload();
    }
    finally {
        saving.value = false;
    }
}
onMounted(async () => {
    await appStore.loadStores();
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "title" },
});
const __VLS_4 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_6 = __VLS_5({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onClick: (__VLS_ctx.openCreate)
};
__VLS_7.slots.default;
var __VLS_7;
const __VLS_12 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_14 = __VLS_13({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_15.slots.default;
const __VLS_16 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    prop: "name",
    label: "名称",
    minWidth: "180",
}));
const __VLS_18 = __VLS_17({
    prop: "name",
    label: "名称",
    minWidth: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
__VLS_19.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_19.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.name);
    if (row.isHeadquarters) {
        const __VLS_20 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
            size: "small",
            type: "warning",
        }));
        const __VLS_22 = __VLS_21({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_21));
        __VLS_23.slots.default;
        var __VLS_23;
    }
}
var __VLS_19;
const __VLS_24 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    prop: "address",
    label: "地址",
    minWidth: "200",
}));
const __VLS_26 = __VLS_25({
    prop: "address",
    label: "地址",
    minWidth: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
const __VLS_28 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    prop: "phone",
    label: "电话",
    width: "140",
}));
const __VLS_30 = __VLS_29({
    prop: "phone",
    label: "电话",
    width: "140",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
const __VLS_32 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    label: "状态",
    width: "100",
}));
const __VLS_34 = __VLS_33({
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
__VLS_35.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_35.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_36 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
        type: (row.isActive ? 'success' : 'info'),
    }));
    const __VLS_38 = __VLS_37({
        type: (row.isActive ? 'success' : 'info'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_37));
    __VLS_39.slots.default;
    (row.isActive ? '营业中' : '已停');
    var __VLS_39;
}
var __VLS_35;
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    label: "操作",
    width: "120",
}));
const __VLS_42 = __VLS_41({
    label: "操作",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_43.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_43.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_44 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_46 = __VLS_45({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_45));
    let __VLS_48;
    let __VLS_49;
    let __VLS_50;
    const __VLS_51 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openEdit(row);
        }
    };
    __VLS_47.slots.default;
    var __VLS_47;
}
var __VLS_43;
var __VLS_15;
var __VLS_3;
const __VLS_52 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '新建分店' : '编辑门店'),
    width: "480px",
}));
const __VLS_54 = __VLS_53({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '新建分店' : '编辑门店'),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_55.slots.default;
const __VLS_56 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}));
const __VLS_58 = __VLS_57({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_60 = {};
__VLS_59.slots.default;
const __VLS_62 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_63 = __VLS_asFunctionalComponent(__VLS_62, new __VLS_62({
    label: "名称",
    prop: "name",
}));
const __VLS_64 = __VLS_63({
    label: "名称",
    prop: "name",
}, ...__VLS_functionalComponentArgsRest(__VLS_63));
__VLS_65.slots.default;
const __VLS_66 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_67 = __VLS_asFunctionalComponent(__VLS_66, new __VLS_66({
    modelValue: (__VLS_ctx.form.name),
}));
const __VLS_68 = __VLS_67({
    modelValue: (__VLS_ctx.form.name),
}, ...__VLS_functionalComponentArgsRest(__VLS_67));
var __VLS_65;
const __VLS_70 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_71 = __VLS_asFunctionalComponent(__VLS_70, new __VLS_70({
    label: "地址",
}));
const __VLS_72 = __VLS_71({
    label: "地址",
}, ...__VLS_functionalComponentArgsRest(__VLS_71));
__VLS_73.slots.default;
const __VLS_74 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_75 = __VLS_asFunctionalComponent(__VLS_74, new __VLS_74({
    modelValue: (__VLS_ctx.form.address),
}));
const __VLS_76 = __VLS_75({
    modelValue: (__VLS_ctx.form.address),
}, ...__VLS_functionalComponentArgsRest(__VLS_75));
var __VLS_73;
const __VLS_78 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_79 = __VLS_asFunctionalComponent(__VLS_78, new __VLS_78({
    label: "电话",
}));
const __VLS_80 = __VLS_79({
    label: "电话",
}, ...__VLS_functionalComponentArgsRest(__VLS_79));
__VLS_81.slots.default;
const __VLS_82 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_83 = __VLS_asFunctionalComponent(__VLS_82, new __VLS_82({
    modelValue: (__VLS_ctx.form.phone),
}));
const __VLS_84 = __VLS_83({
    modelValue: (__VLS_ctx.form.phone),
}, ...__VLS_functionalComponentArgsRest(__VLS_83));
var __VLS_81;
if (__VLS_ctx.formMode === 'create') {
    const __VLS_86 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_87 = __VLS_asFunctionalComponent(__VLS_86, new __VLS_86({
        label: "所属总店",
    }));
    const __VLS_88 = __VLS_87({
        label: "所属总店",
    }, ...__VLS_functionalComponentArgsRest(__VLS_87));
    __VLS_89.slots.default;
    const __VLS_90 = {}.ElSelect;
    /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
    // @ts-ignore
    const __VLS_91 = __VLS_asFunctionalComponent(__VLS_90, new __VLS_90({
        modelValue: (__VLS_ctx.form.parentStoreId),
        placeholder: "选择上级总店",
        ...{ style: {} },
    }));
    const __VLS_92 = __VLS_91({
        modelValue: (__VLS_ctx.form.parentStoreId),
        placeholder: "选择上级总店",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_91));
    __VLS_93.slots.default;
    for (const [s] of __VLS_getVForSourceType((__VLS_ctx.headquarters))) {
        const __VLS_94 = {}.ElOption;
        /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
        // @ts-ignore
        const __VLS_95 = __VLS_asFunctionalComponent(__VLS_94, new __VLS_94({
            key: (s.id),
            label: (s.name),
            value: (s.id),
        }));
        const __VLS_96 = __VLS_95({
            key: (s.id),
            label: (s.name),
            value: (s.id),
        }, ...__VLS_functionalComponentArgsRest(__VLS_95));
    }
    var __VLS_93;
    var __VLS_89;
}
if (__VLS_ctx.formMode === 'edit') {
    const __VLS_98 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_99 = __VLS_asFunctionalComponent(__VLS_98, new __VLS_98({
        label: "状态",
    }));
    const __VLS_100 = __VLS_99({
        label: "状态",
    }, ...__VLS_functionalComponentArgsRest(__VLS_99));
    __VLS_101.slots.default;
    const __VLS_102 = {}.ElSwitch;
    /** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
    // @ts-ignore
    const __VLS_103 = __VLS_asFunctionalComponent(__VLS_102, new __VLS_102({
        modelValue: (__VLS_ctx.form.isActive),
        activeText: "营业中",
        inactiveText: "停业",
    }));
    const __VLS_104 = __VLS_103({
        modelValue: (__VLS_ctx.form.isActive),
        activeText: "营业中",
        inactiveText: "停业",
    }, ...__VLS_functionalComponentArgsRest(__VLS_103));
    var __VLS_101;
}
var __VLS_59;
{
    const { footer: __VLS_thisSlot } = __VLS_55.slots;
    const __VLS_106 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_107 = __VLS_asFunctionalComponent(__VLS_106, new __VLS_106({
        ...{ 'onClick': {} },
    }));
    const __VLS_108 = __VLS_107({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_107));
    let __VLS_110;
    let __VLS_111;
    let __VLS_112;
    const __VLS_113 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_109.slots.default;
    var __VLS_109;
    const __VLS_114 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_115 = __VLS_asFunctionalComponent(__VLS_114, new __VLS_114({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_116 = __VLS_115({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_115));
    let __VLS_118;
    let __VLS_119;
    let __VLS_120;
    const __VLS_121 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_117.slots.default;
    var __VLS_117;
}
var __VLS_55;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
// @ts-ignore
var __VLS_61 = __VLS_60;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            rows: rows,
            loading: loading,
            saving: saving,
            formOpen: formOpen,
            formMode: formMode,
            formRef: formRef,
            form: form,
            rules: rules,
            headquarters: headquarters,
            openCreate: openCreate,
            openEdit: openEdit,
            save: save,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
