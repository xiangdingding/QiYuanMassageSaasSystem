import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, User } from '@element-plus/icons-vue';
import { membersApi, ordersApi, servicesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import PickTechnicianDialog from '@/views/components/PickTechnicianDialog.vue';
import CheckoutDialog from '@/views/components/CheckoutDialog.vue';
const appStore = useAppStore();
const services = ref([]);
const technicians = ref([]);
const serviceFilter = ref('');
const cart = reactive([]);
const member = ref(null);
const memberKeyword = ref('');
const pickOpen = ref(false);
const pickedService = ref(null);
const checkoutOpen = ref(false);
const receiptOpen = ref(false);
const checkingOut = ref(false);
const lastOrder = ref(null);
const filteredServices = computed(() => {
    const k = serviceFilter.value.trim().toLowerCase();
    if (!k)
        return services.value;
    return services.value.filter((s) => s.code.toLowerCase().includes(k) || s.name.toLowerCase().includes(k));
});
const total = computed(() => cart.reduce((sum, c) => sum + c.unitPrice * c.quantity, 0));
const memberDiscount = computed(() => {
    if (!member.value || member.value.discount >= 1)
        return 0;
    return Math.round(total.value * (1 - member.value.discount) * 100) / 100;
});
const payable = computed(() => Math.max(0, total.value - memberDiscount.value));
const canCheckout = computed(() => cart.length > 0 && cart.every((c) => c.technicianId != null) && !!appStore.activeStoreId);
function payMethodLabel(m) {
    return {
        Cash: '现金', MemberCard: '会员卡', Wechat: '微信', Alipay: '支付宝', BankCard: '银行卡', Unpaid: '未支付'
    }[m] ?? m;
}
async function loadCatalog() {
    const [s, t] = await Promise.all([
        servicesApi.list(false),
        staffApi.list({ role: 'Technician', pageSize: 200 })
    ]);
    services.value = s;
    technicians.value = t.items;
}
function onPickService(s) {
    pickedService.value = s;
    pickOpen.value = true;
}
function onTechnicianPicked(payload) {
    if (!pickedService.value)
        return;
    const s = pickedService.value;
    const unit = member.value ? s.memberPrice : s.price;
    cart.push({
        serviceId: s.id,
        serviceName: s.name,
        technicianId: payload.technicianId,
        unitPrice: unit,
        quantity: payload.quantity,
        durationMinutes: s.durationMinutes
    });
    pickOpen.value = false;
}
async function lookupMember() {
    const k = memberKeyword.value.trim();
    if (!k)
        return;
    const data = await membersApi.list({ keyword: k, pageSize: 5 });
    if (data.items.length === 0) {
        ElMessage.warning('未找到会员');
        return;
    }
    member.value = data.items[0];
    // 切换会员后重算价格
    for (const c of cart) {
        const svc = services.value.find((s) => s.id === c.serviceId);
        if (svc)
            c.unitPrice = member.value ? svc.memberPrice : svc.price;
    }
    ElMessage.success(`已关联会员 ${member.value.name || member.value.cardNo}`);
}
function openCheckout() {
    if (!canCheckout.value) {
        ElMessage.warning('请确保所有项目都指派了技师');
        return;
    }
    checkoutOpen.value = true;
}
async function doCheckout(payload) {
    if (!appStore.activeStoreId) {
        ElMessage.error('未选择门店');
        return;
    }
    checkingOut.value = true;
    try {
        const created = await ordersApi.create({
            storeId: appStore.activeStoreId,
            memberId: member.value?.id ?? null,
            items: cart.map((c) => ({
                serviceId: c.serviceId,
                technicianId: c.technicianId,
                quantity: c.quantity
            })),
            remark: null
        });
        const checked = await ordersApi.checkout(created.id, {
            payMethod: payload.payMethod,
            paidAmount: payload.paidAmount,
            discountAmount: 0,
            remark: payload.remark
        });
        lastOrder.value = checked;
        checkoutOpen.value = false;
        receiptOpen.value = true;
        ElMessage.success('结账成功');
    }
    catch {
        /* http 已弹错 */
    }
    finally {
        checkingOut.value = false;
    }
}
async function resetAll() {
    if (cart.length > 0) {
        await ElMessageBox.confirm('确认清空当前订单？', '提示', { type: 'warning' }).catch(() => null);
    }
    cart.splice(0, cart.length);
    member.value = null;
    memberKeyword.value = '';
    lastOrder.value = null;
}
onMounted(async () => {
    await appStore.loadStores();
    await loadCatalog();
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['right']} */ ;
/** @type {__VLS_StyleScopedClasses['cart']} */ ;
/** @type {__VLS_StyleScopedClasses['service-card']} */ ;
/** @type {__VLS_StyleScopedClasses['el-card__body']} */ ;
/** @type {__VLS_StyleScopedClasses['m-line']} */ ;
/** @type {__VLS_StyleScopedClasses['total-line']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "pos" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "left" },
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
{
    const { header: __VLS_thisSlot } = __VLS_3.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "header-row" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    const __VLS_4 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        modelValue: (__VLS_ctx.serviceFilter),
        placeholder: "按编码或名称过滤",
        size: "default",
        clearable: true,
        ...{ style: {} },
        prefixIcon: (__VLS_ctx.Search),
    }));
    const __VLS_6 = __VLS_5({
        modelValue: (__VLS_ctx.serviceFilter),
        placeholder: "按编码或名称过滤",
        size: "default",
        clearable: true,
        ...{ style: {} },
        prefixIcon: (__VLS_ctx.Search),
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "services-grid" },
});
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.filteredServices))) {
    const __VLS_8 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
        ...{ 'onClick': {} },
        key: (s.id),
        ...{ class: "service-card" },
        shadow: "hover",
    }));
    const __VLS_10 = __VLS_9({
        ...{ 'onClick': {} },
        key: (s.id),
        ...{ class: "service-card" },
        shadow: "hover",
    }, ...__VLS_functionalComponentArgsRest(__VLS_9));
    let __VLS_12;
    let __VLS_13;
    let __VLS_14;
    const __VLS_15 = {
        onClick: (...[$event]) => {
            __VLS_ctx.onPickService(s);
        }
    };
    __VLS_11.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "svc-name" },
    });
    (s.name);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "svc-meta" },
    });
    const __VLS_16 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
        size: "small",
    }));
    const __VLS_18 = __VLS_17({
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_17));
    __VLS_19.slots.default;
    (s.durationMinutes);
    var __VLS_19;
    const __VLS_20 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
        size: "small",
        type: "success",
    }));
    const __VLS_22 = __VLS_21({
        size: "small",
        type: "success",
    }, ...__VLS_functionalComponentArgsRest(__VLS_21));
    __VLS_23.slots.default;
    (s.price.toFixed(2));
    var __VLS_23;
    if (__VLS_ctx.member) {
        const __VLS_24 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
            size: "small",
            type: "warning",
        }));
        const __VLS_26 = __VLS_25({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_25));
        __VLS_27.slots.default;
        (s.memberPrice.toFixed(2));
        var __VLS_27;
    }
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "svc-code" },
    });
    (s.code);
    var __VLS_11;
}
var __VLS_3;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "right" },
});
const __VLS_28 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    shadow: "never",
    ...{ class: "cart" },
}));
const __VLS_30 = __VLS_29({
    shadow: "never",
    ...{ class: "cart" },
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
__VLS_31.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_31.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "header-row" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    if (__VLS_ctx.cart.length > 0 || __VLS_ctx.member) {
        const __VLS_32 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }));
        const __VLS_34 = __VLS_33({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
        }, ...__VLS_functionalComponentArgsRest(__VLS_33));
        let __VLS_36;
        let __VLS_37;
        let __VLS_38;
        const __VLS_39 = {
            onClick: (__VLS_ctx.resetAll)
        };
        __VLS_35.slots.default;
        var __VLS_35;
    }
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "member-row" },
});
const __VLS_40 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.memberKeyword),
    placeholder: "会员卡号 / 手机号",
    clearable: true,
    prefixIcon: (__VLS_ctx.User),
    ...{ style: {} },
}));
const __VLS_42 = __VLS_41({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.memberKeyword),
    placeholder: "会员卡号 / 手机号",
    clearable: true,
    prefixIcon: (__VLS_ctx.User),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
let __VLS_44;
let __VLS_45;
let __VLS_46;
const __VLS_47 = {
    onKeyup: (__VLS_ctx.lookupMember)
};
var __VLS_43;
const __VLS_48 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Search),
}));
const __VLS_50 = __VLS_49({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Search),
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
let __VLS_52;
let __VLS_53;
let __VLS_54;
const __VLS_55 = {
    onClick: (__VLS_ctx.lookupMember)
};
__VLS_51.slots.default;
var __VLS_51;
if (__VLS_ctx.member) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "member-info" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-line" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.member.name || __VLS_ctx.member.cardNo);
    const __VLS_56 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
        size: "small",
    }));
    const __VLS_58 = __VLS_57({
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_57));
    __VLS_59.slots.default;
    (__VLS_ctx.member.balance.toFixed(2));
    var __VLS_59;
    if (__VLS_ctx.member.discount < 1) {
        const __VLS_60 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
            size: "small",
            type: "warning",
        }));
        const __VLS_62 = __VLS_61({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_61));
        __VLS_63.slots.default;
        ((__VLS_ctx.member.discount * 10).toFixed(1));
        var __VLS_63;
    }
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-line muted" },
    });
    (__VLS_ctx.member.phone);
    (__VLS_ctx.member.totalConsumed.toFixed(2));
    const __VLS_64 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
    }));
    const __VLS_66 = __VLS_65({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    let __VLS_68;
    let __VLS_69;
    let __VLS_70;
    const __VLS_71 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.member))
                return;
            __VLS_ctx.member = null;
            __VLS_ctx.memberKeyword = '';
        }
    };
    __VLS_67.slots.default;
    var __VLS_67;
}
const __VLS_72 = {}.ElDivider;
/** @type {[typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    ...{ style: {} },
}));
const __VLS_74 = __VLS_73({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
if (__VLS_ctx.cart.length === 0) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "empty" },
    });
}
else {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "cart-list" },
    });
    for (const [it, idx] of __VLS_getVForSourceType((__VLS_ctx.cart))) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            key: (idx),
            ...{ class: "cart-item" },
        });
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "ci-line" },
        });
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "ci-name" },
        });
        (it.serviceName);
        const __VLS_76 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
            size: "small",
        }));
        const __VLS_78 = __VLS_77({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
            size: "small",
        }, ...__VLS_functionalComponentArgsRest(__VLS_77));
        let __VLS_80;
        let __VLS_81;
        let __VLS_82;
        const __VLS_83 = {
            onClick: (...[$event]) => {
                if (!!(__VLS_ctx.cart.length === 0))
                    return;
                __VLS_ctx.cart.splice(idx, 1);
            }
        };
        __VLS_79.slots.default;
        var __VLS_79;
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "ci-meta" },
        });
        const __VLS_84 = {}.ElSelect;
        /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
        // @ts-ignore
        const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
            modelValue: (it.technicianId),
            placeholder: "选择技师",
            size: "small",
            ...{ style: {} },
            filterable: true,
        }));
        const __VLS_86 = __VLS_85({
            modelValue: (it.technicianId),
            placeholder: "选择技师",
            size: "small",
            ...{ style: {} },
            filterable: true,
        }, ...__VLS_functionalComponentArgsRest(__VLS_85));
        __VLS_87.slots.default;
        for (const [t] of __VLS_getVForSourceType((__VLS_ctx.technicians))) {
            const __VLS_88 = {}.ElOption;
            /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
            // @ts-ignore
            const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
                key: (t.id),
                label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
                value: (t.id),
            }));
            const __VLS_90 = __VLS_89({
                key: (t.id),
                label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
                value: (t.id),
            }, ...__VLS_functionalComponentArgsRest(__VLS_89));
        }
        var __VLS_87;
        const __VLS_92 = {}.ElInputNumber;
        /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
        // @ts-ignore
        const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
            modelValue: (it.quantity),
            min: (1),
            max: (20),
            size: "small",
            controlsPosition: "right",
            ...{ style: {} },
        }));
        const __VLS_94 = __VLS_93({
            modelValue: (it.quantity),
            min: (1),
            max: (20),
            size: "small",
            controlsPosition: "right",
            ...{ style: {} },
        }, ...__VLS_functionalComponentArgsRest(__VLS_93));
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "ci-price" },
        });
        ((it.unitPrice * it.quantity).toFixed(2));
    }
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "totals" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "total-line" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "total-amount" },
});
(__VLS_ctx.total.toFixed(2));
if (__VLS_ctx.member && __VLS_ctx.member.discount < 1) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "total-line muted" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    (__VLS_ctx.memberDiscount.toFixed(2));
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "total-line" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "total-amount" },
});
(__VLS_ctx.payable.toFixed(2));
const __VLS_96 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    ...{ 'onClick': {} },
    type: "primary",
    size: "large",
    disabled: (!__VLS_ctx.canCheckout),
    loading: (__VLS_ctx.checkingOut),
    ...{ style: {} },
}));
const __VLS_98 = __VLS_97({
    ...{ 'onClick': {} },
    type: "primary",
    size: "large",
    disabled: (!__VLS_ctx.canCheckout),
    loading: (__VLS_ctx.checkingOut),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
let __VLS_100;
let __VLS_101;
let __VLS_102;
const __VLS_103 = {
    onClick: (__VLS_ctx.openCheckout)
};
__VLS_99.slots.default;
var __VLS_99;
var __VLS_31;
/** @type {[typeof PickTechnicianDialog, ]} */ ;
// @ts-ignore
const __VLS_104 = __VLS_asFunctionalComponent(PickTechnicianDialog, new PickTechnicianDialog({
    ...{ 'onConfirm': {} },
    modelValue: (__VLS_ctx.pickOpen),
    service: (__VLS_ctx.pickedService),
    technicians: (__VLS_ctx.technicians),
    isMember: (!!__VLS_ctx.member),
}));
const __VLS_105 = __VLS_104({
    ...{ 'onConfirm': {} },
    modelValue: (__VLS_ctx.pickOpen),
    service: (__VLS_ctx.pickedService),
    technicians: (__VLS_ctx.technicians),
    isMember: (!!__VLS_ctx.member),
}, ...__VLS_functionalComponentArgsRest(__VLS_104));
let __VLS_107;
let __VLS_108;
let __VLS_109;
const __VLS_110 = {
    onConfirm: (__VLS_ctx.onTechnicianPicked)
};
var __VLS_106;
/** @type {[typeof CheckoutDialog, ]} */ ;
// @ts-ignore
const __VLS_111 = __VLS_asFunctionalComponent(CheckoutDialog, new CheckoutDialog({
    ...{ 'onSubmit': {} },
    modelValue: (__VLS_ctx.checkoutOpen),
    total: (__VLS_ctx.total),
    payable: (__VLS_ctx.payable),
    hasMember: (!!__VLS_ctx.member),
    memberBalance: (__VLS_ctx.member?.balance ?? 0),
    loading: (__VLS_ctx.checkingOut),
}));
const __VLS_112 = __VLS_111({
    ...{ 'onSubmit': {} },
    modelValue: (__VLS_ctx.checkoutOpen),
    total: (__VLS_ctx.total),
    payable: (__VLS_ctx.payable),
    hasMember: (!!__VLS_ctx.member),
    memberBalance: (__VLS_ctx.member?.balance ?? 0),
    loading: (__VLS_ctx.checkingOut),
}, ...__VLS_functionalComponentArgsRest(__VLS_111));
let __VLS_114;
let __VLS_115;
let __VLS_116;
const __VLS_117 = {
    onSubmit: (__VLS_ctx.doCheckout)
};
var __VLS_113;
const __VLS_118 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_119 = __VLS_asFunctionalComponent(__VLS_118, new __VLS_118({
    modelValue: (__VLS_ctx.receiptOpen),
    title: "结账成功",
    width: "420px",
}));
const __VLS_120 = __VLS_119({
    modelValue: (__VLS_ctx.receiptOpen),
    title: "结账成功",
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_119));
__VLS_121.slots.default;
if (__VLS_ctx.lastOrder) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.lastOrder.orderNo);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.lastOrder.total.toFixed(2));
    __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.lastOrder.paidAmount.toFixed(2));
    (__VLS_ctx.payMethodLabel(__VLS_ctx.lastOrder.payMethod));
    if (__VLS_ctx.lastOrder.discountAmount > 0) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({});
        (__VLS_ctx.lastOrder.discountAmount.toFixed(2));
    }
    const __VLS_122 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_123 = __VLS_asFunctionalComponent(__VLS_122, new __VLS_122({
        data: (__VLS_ctx.lastOrder.items),
        size: "small",
    }));
    const __VLS_124 = __VLS_123({
        data: (__VLS_ctx.lastOrder.items),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_123));
    __VLS_125.slots.default;
    const __VLS_126 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_127 = __VLS_asFunctionalComponent(__VLS_126, new __VLS_126({
        prop: "serviceName",
        label: "项目",
    }));
    const __VLS_128 = __VLS_127({
        prop: "serviceName",
        label: "项目",
    }, ...__VLS_functionalComponentArgsRest(__VLS_127));
    const __VLS_130 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_131 = __VLS_asFunctionalComponent(__VLS_130, new __VLS_130({
        prop: "technicianName",
        label: "技师",
        width: "100",
    }));
    const __VLS_132 = __VLS_131({
        prop: "technicianName",
        label: "技师",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_131));
    const __VLS_134 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_135 = __VLS_asFunctionalComponent(__VLS_134, new __VLS_134({
        label: "金额",
        width: "100",
    }));
    const __VLS_136 = __VLS_135({
        label: "金额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_135));
    __VLS_137.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_137.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.itemTotal.toFixed(2));
    }
    var __VLS_137;
    var __VLS_125;
}
{
    const { footer: __VLS_thisSlot } = __VLS_121.slots;
    const __VLS_138 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_139 = __VLS_asFunctionalComponent(__VLS_138, new __VLS_138({
        ...{ 'onClick': {} },
        type: "primary",
    }));
    const __VLS_140 = __VLS_139({
        ...{ 'onClick': {} },
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_139));
    let __VLS_142;
    let __VLS_143;
    let __VLS_144;
    const __VLS_145 = {
        onClick: (...[$event]) => {
            __VLS_ctx.receiptOpen = false;
            __VLS_ctx.resetAll();
        }
    };
    __VLS_141.slots.default;
    var __VLS_141;
}
var __VLS_121;
/** @type {__VLS_StyleScopedClasses['pos']} */ ;
/** @type {__VLS_StyleScopedClasses['left']} */ ;
/** @type {__VLS_StyleScopedClasses['header-row']} */ ;
/** @type {__VLS_StyleScopedClasses['services-grid']} */ ;
/** @type {__VLS_StyleScopedClasses['service-card']} */ ;
/** @type {__VLS_StyleScopedClasses['svc-name']} */ ;
/** @type {__VLS_StyleScopedClasses['svc-meta']} */ ;
/** @type {__VLS_StyleScopedClasses['svc-code']} */ ;
/** @type {__VLS_StyleScopedClasses['right']} */ ;
/** @type {__VLS_StyleScopedClasses['cart']} */ ;
/** @type {__VLS_StyleScopedClasses['header-row']} */ ;
/** @type {__VLS_StyleScopedClasses['member-row']} */ ;
/** @type {__VLS_StyleScopedClasses['member-info']} */ ;
/** @type {__VLS_StyleScopedClasses['m-line']} */ ;
/** @type {__VLS_StyleScopedClasses['m-line']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['empty']} */ ;
/** @type {__VLS_StyleScopedClasses['cart-list']} */ ;
/** @type {__VLS_StyleScopedClasses['cart-item']} */ ;
/** @type {__VLS_StyleScopedClasses['ci-line']} */ ;
/** @type {__VLS_StyleScopedClasses['ci-name']} */ ;
/** @type {__VLS_StyleScopedClasses['ci-meta']} */ ;
/** @type {__VLS_StyleScopedClasses['ci-price']} */ ;
/** @type {__VLS_StyleScopedClasses['totals']} */ ;
/** @type {__VLS_StyleScopedClasses['total-line']} */ ;
/** @type {__VLS_StyleScopedClasses['total-amount']} */ ;
/** @type {__VLS_StyleScopedClasses['total-line']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['total-line']} */ ;
/** @type {__VLS_StyleScopedClasses['total-amount']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Search: Search,
            User: User,
            PickTechnicianDialog: PickTechnicianDialog,
            CheckoutDialog: CheckoutDialog,
            technicians: technicians,
            serviceFilter: serviceFilter,
            cart: cart,
            member: member,
            memberKeyword: memberKeyword,
            pickOpen: pickOpen,
            pickedService: pickedService,
            checkoutOpen: checkoutOpen,
            receiptOpen: receiptOpen,
            checkingOut: checkingOut,
            lastOrder: lastOrder,
            filteredServices: filteredServices,
            total: total,
            memberDiscount: memberDiscount,
            payable: payable,
            canCheckout: canCheckout,
            payMethodLabel: payMethodLabel,
            onPickService: onPickService,
            onTechnicianPicked: onTechnicianPicked,
            lookupMember: lookupMember,
            openCheckout: openCheckout,
            doCheckout: doCheckout,
            resetAll: resetAll,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
