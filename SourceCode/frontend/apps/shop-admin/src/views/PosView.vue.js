import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, User } from '@element-plus/icons-vue';
import { membersApi, ordersApi, roomsApi, servicesApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useShortcuts } from '@/composables/useShortcuts';
import PickTechnicianDialog from '@/views/components/PickTechnicianDialog.vue';
import CheckoutDialog from '@/views/components/CheckoutDialog.vue';
const appStore = useAppStore();
const services = ref([]);
const technicians = ref([]);
const rooms = ref([]);
const serviceFilter = ref('');
const serviceFilterInput = ref(null);
const memberInput = ref(null);
function quickAddFirst() {
    const first = filteredServices.value[0];
    if (first)
        onPickService(first);
}
function availableRooms(currentRoomId) {
    // 当前 cart 中已选过的房间在其它项目里禁用，自己当前选的留可见
    const usedElsewhere = new Set(cart.filter((c) => c.roomId !== null && c.roomId !== currentRoomId).map((c) => c.roomId));
    return rooms.value.filter((r) => !usedElsewhere.has(r.id));
}
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
    const sid = appStore.activeStoreId;
    const [s, t, r] = await Promise.all([
        servicesApi.list(false),
        staffApi.list({ role: 'Technician', pageSize: 200, storeId: sid ?? undefined }),
        sid ? roomsApi.list(sid).catch(() => []) : Promise.resolve([])
    ]);
    services.value = s;
    technicians.value = t.items;
    rooms.value = r;
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
        roomId: null,
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
                quantity: c.quantity,
                roomId: c.roomId
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
useShortcuts({
    onMemberSearch: () => memberInput.value?.focus(),
    onRefresh: () => loadCatalog(),
    onPrimary: () => { if (canCheckout.value)
        openCheckout(); }
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
    role: "region",
    'aria-label': "收银台",
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
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({
        ...{ class: "card-title" },
    });
    const __VLS_4 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        ...{ 'onKeyup': {} },
        ref: "serviceFilterInput",
        modelValue: (__VLS_ctx.serviceFilter),
        placeholder: "按编码或名称过滤，回车快速加入第一项",
        size: "default",
        clearable: true,
        ...{ style: {} },
        prefixIcon: (__VLS_ctx.Search),
        'aria-label': "搜索服务项目，按回车快速添加第一项",
    }));
    const __VLS_6 = __VLS_5({
        ...{ 'onKeyup': {} },
        ref: "serviceFilterInput",
        modelValue: (__VLS_ctx.serviceFilter),
        placeholder: "按编码或名称过滤，回车快速加入第一项",
        size: "default",
        clearable: true,
        ...{ style: {} },
        prefixIcon: (__VLS_ctx.Search),
        'aria-label': "搜索服务项目，按回车快速添加第一项",
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
    let __VLS_8;
    let __VLS_9;
    let __VLS_10;
    const __VLS_11 = {
        onKeyup: (__VLS_ctx.quickAddFirst)
    };
    /** @type {typeof __VLS_ctx.serviceFilterInput} */ ;
    var __VLS_12 = {};
    var __VLS_7;
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "services-grid" },
    role: "list",
    'aria-label': "服务项目列表",
});
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.filteredServices))) {
    const __VLS_14 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_15 = __VLS_asFunctionalComponent(__VLS_14, new __VLS_14({
        ...{ 'onClick': {} },
        ...{ 'onKeyup': {} },
        ...{ 'onKeyup': {} },
        key: (s.id),
        ...{ class: "service-card" },
        shadow: "hover",
        tabindex: "0",
        role: "button",
        'aria-label': (`服务 ${s.name}，时长 ${s.durationMinutes} 分钟，标准价 ${s.price.toFixed(2)} 元${__VLS_ctx.member ? '，会员价 ' + s.memberPrice.toFixed(2) + ' 元' : ''}，按回车添加`),
    }));
    const __VLS_16 = __VLS_15({
        ...{ 'onClick': {} },
        ...{ 'onKeyup': {} },
        ...{ 'onKeyup': {} },
        key: (s.id),
        ...{ class: "service-card" },
        shadow: "hover",
        tabindex: "0",
        role: "button",
        'aria-label': (`服务 ${s.name}，时长 ${s.durationMinutes} 分钟，标准价 ${s.price.toFixed(2)} 元${__VLS_ctx.member ? '，会员价 ' + s.memberPrice.toFixed(2) + ' 元' : ''}，按回车添加`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_15));
    let __VLS_18;
    let __VLS_19;
    let __VLS_20;
    const __VLS_21 = {
        onClick: (...[$event]) => {
            __VLS_ctx.onPickService(s);
        }
    };
    const __VLS_22 = {
        onKeyup: (...[$event]) => {
            __VLS_ctx.onPickService(s);
        }
    };
    const __VLS_23 = {
        onKeyup: (...[$event]) => {
            __VLS_ctx.onPickService(s);
        }
    };
    __VLS_17.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "svc-name" },
    });
    (s.name);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "svc-meta" },
    });
    const __VLS_24 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
        size: "small",
    }));
    const __VLS_26 = __VLS_25({
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_25));
    __VLS_27.slots.default;
    (s.durationMinutes);
    var __VLS_27;
    const __VLS_28 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
        size: "small",
        type: "success",
    }));
    const __VLS_30 = __VLS_29({
        size: "small",
        type: "success",
    }, ...__VLS_functionalComponentArgsRest(__VLS_29));
    __VLS_31.slots.default;
    (s.price.toFixed(2));
    var __VLS_31;
    if (__VLS_ctx.member) {
        const __VLS_32 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
            size: "small",
            type: "warning",
        }));
        const __VLS_34 = __VLS_33({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_33));
        __VLS_35.slots.default;
        (s.memberPrice.toFixed(2));
        var __VLS_35;
    }
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "svc-code" },
    });
    (s.code);
    var __VLS_17;
}
var __VLS_3;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "right" },
});
const __VLS_36 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    shadow: "never",
    ...{ class: "cart" },
}));
const __VLS_38 = __VLS_37({
    shadow: "never",
    ...{ class: "cart" },
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
__VLS_39.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_39.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "header-row" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({
        ...{ class: "card-title" },
    });
    if (__VLS_ctx.cart.length > 0 || __VLS_ctx.member) {
        const __VLS_40 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
            'aria-label': "清空当前订单",
        }));
        const __VLS_42 = __VLS_41({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
            'aria-label': "清空当前订单",
        }, ...__VLS_functionalComponentArgsRest(__VLS_41));
        let __VLS_44;
        let __VLS_45;
        let __VLS_46;
        const __VLS_47 = {
            onClick: (__VLS_ctx.resetAll)
        };
        __VLS_43.slots.default;
        var __VLS_43;
    }
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "member-row" },
});
const __VLS_48 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    ...{ 'onKeyup': {} },
    ref: "memberInput",
    modelValue: (__VLS_ctx.memberKeyword),
    placeholder: "会员卡号 / 手机号",
    clearable: true,
    prefixIcon: (__VLS_ctx.User),
    ...{ style: {} },
    'aria-label': "会员卡号或手机号，按 F2 快速聚焦",
}));
const __VLS_50 = __VLS_49({
    ...{ 'onKeyup': {} },
    ref: "memberInput",
    modelValue: (__VLS_ctx.memberKeyword),
    placeholder: "会员卡号 / 手机号",
    clearable: true,
    prefixIcon: (__VLS_ctx.User),
    ...{ style: {} },
    'aria-label': "会员卡号或手机号，按 F2 快速聚焦",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
let __VLS_52;
let __VLS_53;
let __VLS_54;
const __VLS_55 = {
    onKeyup: (__VLS_ctx.lookupMember)
};
/** @type {typeof __VLS_ctx.memberInput} */ ;
var __VLS_56 = {};
var __VLS_51;
const __VLS_58 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_59 = __VLS_asFunctionalComponent(__VLS_58, new __VLS_58({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Search),
    'aria-label': "查询会员",
}));
const __VLS_60 = __VLS_59({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Search),
    'aria-label': "查询会员",
}, ...__VLS_functionalComponentArgsRest(__VLS_59));
let __VLS_62;
let __VLS_63;
let __VLS_64;
const __VLS_65 = {
    onClick: (__VLS_ctx.lookupMember)
};
__VLS_61.slots.default;
var __VLS_61;
if (__VLS_ctx.member) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "member-info" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-line" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.member.name || __VLS_ctx.member.cardNo);
    const __VLS_66 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_67 = __VLS_asFunctionalComponent(__VLS_66, new __VLS_66({
        size: "small",
    }));
    const __VLS_68 = __VLS_67({
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_67));
    __VLS_69.slots.default;
    (__VLS_ctx.member.balance.toFixed(2));
    var __VLS_69;
    if (__VLS_ctx.member.discount < 1) {
        const __VLS_70 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_71 = __VLS_asFunctionalComponent(__VLS_70, new __VLS_70({
            size: "small",
            type: "warning",
        }));
        const __VLS_72 = __VLS_71({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_71));
        __VLS_73.slots.default;
        ((__VLS_ctx.member.discount * 10).toFixed(1));
        var __VLS_73;
    }
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "m-line muted" },
    });
    (__VLS_ctx.member.phone);
    (__VLS_ctx.member.totalConsumed.toFixed(2));
    const __VLS_74 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_75 = __VLS_asFunctionalComponent(__VLS_74, new __VLS_74({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
    }));
    const __VLS_76 = __VLS_75({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
    }, ...__VLS_functionalComponentArgsRest(__VLS_75));
    let __VLS_78;
    let __VLS_79;
    let __VLS_80;
    const __VLS_81 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.member))
                return;
            __VLS_ctx.member = null;
            __VLS_ctx.memberKeyword = '';
        }
    };
    __VLS_77.slots.default;
    var __VLS_77;
}
const __VLS_82 = {}.ElDivider;
/** @type {[typeof __VLS_components.ElDivider, typeof __VLS_components.elDivider, ]} */ ;
// @ts-ignore
const __VLS_83 = __VLS_asFunctionalComponent(__VLS_82, new __VLS_82({
    ...{ style: {} },
}));
const __VLS_84 = __VLS_83({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_83));
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
        const __VLS_86 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_87 = __VLS_asFunctionalComponent(__VLS_86, new __VLS_86({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
            size: "small",
        }));
        const __VLS_88 = __VLS_87({
            ...{ 'onClick': {} },
            link: true,
            type: "danger",
            size: "small",
        }, ...__VLS_functionalComponentArgsRest(__VLS_87));
        let __VLS_90;
        let __VLS_91;
        let __VLS_92;
        const __VLS_93 = {
            onClick: (...[$event]) => {
                if (!!(__VLS_ctx.cart.length === 0))
                    return;
                __VLS_ctx.cart.splice(idx, 1);
            }
        };
        __VLS_89.slots.default;
        var __VLS_89;
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "ci-meta" },
        });
        const __VLS_94 = {}.ElSelect;
        /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
        // @ts-ignore
        const __VLS_95 = __VLS_asFunctionalComponent(__VLS_94, new __VLS_94({
            modelValue: (it.technicianId),
            placeholder: "选择技师",
            size: "small",
            ...{ style: {} },
            filterable: true,
        }));
        const __VLS_96 = __VLS_95({
            modelValue: (it.technicianId),
            placeholder: "选择技师",
            size: "small",
            ...{ style: {} },
            filterable: true,
        }, ...__VLS_functionalComponentArgsRest(__VLS_95));
        __VLS_97.slots.default;
        for (const [t] of __VLS_getVForSourceType((__VLS_ctx.technicians))) {
            const __VLS_98 = {}.ElOption;
            /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
            // @ts-ignore
            const __VLS_99 = __VLS_asFunctionalComponent(__VLS_98, new __VLS_98({
                key: (t.id),
                label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
                value: (t.id),
            }));
            const __VLS_100 = __VLS_99({
                key: (t.id),
                label: (`${t.employeeNo ?? '-'} · ${t.realName ?? t.username}`),
                value: (t.id),
            }, ...__VLS_functionalComponentArgsRest(__VLS_99));
        }
        var __VLS_97;
        const __VLS_102 = {}.ElSelect;
        /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
        // @ts-ignore
        const __VLS_103 = __VLS_asFunctionalComponent(__VLS_102, new __VLS_102({
            modelValue: (it.roomId),
            placeholder: "房间",
            size: "small",
            ...{ style: {} },
            clearable: true,
        }));
        const __VLS_104 = __VLS_103({
            modelValue: (it.roomId),
            placeholder: "房间",
            size: "small",
            ...{ style: {} },
            clearable: true,
        }, ...__VLS_functionalComponentArgsRest(__VLS_103));
        __VLS_105.slots.default;
        for (const [r] of __VLS_getVForSourceType((__VLS_ctx.availableRooms(it.roomId)))) {
            const __VLS_106 = {}.ElOption;
            /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
            // @ts-ignore
            const __VLS_107 = __VLS_asFunctionalComponent(__VLS_106, new __VLS_106({
                key: (r.id),
                label: (r.roomNo + (r.isOccupied ? '（占用中）' : '')),
                value: (r.id),
                disabled: (r.isOccupied && r.id !== it.roomId),
            }));
            const __VLS_108 = __VLS_107({
                key: (r.id),
                label: (r.roomNo + (r.isOccupied ? '（占用中）' : '')),
                value: (r.id),
                disabled: (r.isOccupied && r.id !== it.roomId),
            }, ...__VLS_functionalComponentArgsRest(__VLS_107));
        }
        var __VLS_105;
        const __VLS_110 = {}.ElInputNumber;
        /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
        // @ts-ignore
        const __VLS_111 = __VLS_asFunctionalComponent(__VLS_110, new __VLS_110({
            modelValue: (it.quantity),
            min: (1),
            max: (20),
            size: "small",
            controlsPosition: "right",
            ...{ style: {} },
        }));
        const __VLS_112 = __VLS_111({
            modelValue: (it.quantity),
            min: (1),
            max: (20),
            size: "small",
            controlsPosition: "right",
            ...{ style: {} },
        }, ...__VLS_functionalComponentArgsRest(__VLS_111));
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
const __VLS_114 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_115 = __VLS_asFunctionalComponent(__VLS_114, new __VLS_114({
    ...{ 'onClick': {} },
    type: "primary",
    size: "large",
    disabled: (!__VLS_ctx.canCheckout),
    loading: (__VLS_ctx.checkingOut),
    ...{ style: {} },
    'aria-label': (`下单并结账，应收 ${__VLS_ctx.payable.toFixed(2)} 元`),
}));
const __VLS_116 = __VLS_115({
    ...{ 'onClick': {} },
    type: "primary",
    size: "large",
    disabled: (!__VLS_ctx.canCheckout),
    loading: (__VLS_ctx.checkingOut),
    ...{ style: {} },
    'aria-label': (`下单并结账，应收 ${__VLS_ctx.payable.toFixed(2)} 元`),
}, ...__VLS_functionalComponentArgsRest(__VLS_115));
let __VLS_118;
let __VLS_119;
let __VLS_120;
const __VLS_121 = {
    onClick: (__VLS_ctx.openCheckout)
};
__VLS_117.slots.default;
(__VLS_ctx.payable.toFixed(2));
var __VLS_117;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "hint" },
    'aria-live': "polite",
});
if (__VLS_ctx.cart.length > 0 && !__VLS_ctx.canCheckout) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
}
var __VLS_39;
/** @type {[typeof PickTechnicianDialog, ]} */ ;
// @ts-ignore
const __VLS_122 = __VLS_asFunctionalComponent(PickTechnicianDialog, new PickTechnicianDialog({
    ...{ 'onConfirm': {} },
    modelValue: (__VLS_ctx.pickOpen),
    service: (__VLS_ctx.pickedService),
    technicians: (__VLS_ctx.technicians),
    isMember: (!!__VLS_ctx.member),
}));
const __VLS_123 = __VLS_122({
    ...{ 'onConfirm': {} },
    modelValue: (__VLS_ctx.pickOpen),
    service: (__VLS_ctx.pickedService),
    technicians: (__VLS_ctx.technicians),
    isMember: (!!__VLS_ctx.member),
}, ...__VLS_functionalComponentArgsRest(__VLS_122));
let __VLS_125;
let __VLS_126;
let __VLS_127;
const __VLS_128 = {
    onConfirm: (__VLS_ctx.onTechnicianPicked)
};
var __VLS_124;
/** @type {[typeof CheckoutDialog, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(CheckoutDialog, new CheckoutDialog({
    ...{ 'onSubmit': {} },
    modelValue: (__VLS_ctx.checkoutOpen),
    total: (__VLS_ctx.total),
    payable: (__VLS_ctx.payable),
    hasMember: (!!__VLS_ctx.member),
    memberBalance: (__VLS_ctx.member?.balance ?? 0),
    loading: (__VLS_ctx.checkingOut),
}));
const __VLS_130 = __VLS_129({
    ...{ 'onSubmit': {} },
    modelValue: (__VLS_ctx.checkoutOpen),
    total: (__VLS_ctx.total),
    payable: (__VLS_ctx.payable),
    hasMember: (!!__VLS_ctx.member),
    memberBalance: (__VLS_ctx.member?.balance ?? 0),
    loading: (__VLS_ctx.checkingOut),
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
let __VLS_132;
let __VLS_133;
let __VLS_134;
const __VLS_135 = {
    onSubmit: (__VLS_ctx.doCheckout)
};
var __VLS_131;
const __VLS_136 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
    modelValue: (__VLS_ctx.receiptOpen),
    title: "结账成功",
    width: "420px",
}));
const __VLS_138 = __VLS_137({
    modelValue: (__VLS_ctx.receiptOpen),
    title: "结账成功",
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_137));
__VLS_139.slots.default;
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
    const __VLS_140 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
        data: (__VLS_ctx.lastOrder.items),
        size: "small",
    }));
    const __VLS_142 = __VLS_141({
        data: (__VLS_ctx.lastOrder.items),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_141));
    __VLS_143.slots.default;
    const __VLS_144 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
        prop: "serviceName",
        label: "项目",
    }));
    const __VLS_146 = __VLS_145({
        prop: "serviceName",
        label: "项目",
    }, ...__VLS_functionalComponentArgsRest(__VLS_145));
    const __VLS_148 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
        prop: "technicianName",
        label: "技师",
        width: "100",
    }));
    const __VLS_150 = __VLS_149({
        prop: "technicianName",
        label: "技师",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_149));
    const __VLS_152 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
        label: "金额",
        width: "100",
    }));
    const __VLS_154 = __VLS_153({
        label: "金额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_153));
    __VLS_155.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_155.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.itemTotal.toFixed(2));
    }
    var __VLS_155;
    var __VLS_143;
}
{
    const { footer: __VLS_thisSlot } = __VLS_139.slots;
    const __VLS_156 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
        ...{ 'onClick': {} },
        type: "primary",
    }));
    const __VLS_158 = __VLS_157({
        ...{ 'onClick': {} },
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_157));
    let __VLS_160;
    let __VLS_161;
    let __VLS_162;
    const __VLS_163 = {
        onClick: (...[$event]) => {
            __VLS_ctx.receiptOpen = false;
            __VLS_ctx.resetAll();
        }
    };
    __VLS_159.slots.default;
    var __VLS_159;
}
var __VLS_139;
/** @type {__VLS_StyleScopedClasses['pos']} */ ;
/** @type {__VLS_StyleScopedClasses['left']} */ ;
/** @type {__VLS_StyleScopedClasses['header-row']} */ ;
/** @type {__VLS_StyleScopedClasses['card-title']} */ ;
/** @type {__VLS_StyleScopedClasses['services-grid']} */ ;
/** @type {__VLS_StyleScopedClasses['service-card']} */ ;
/** @type {__VLS_StyleScopedClasses['svc-name']} */ ;
/** @type {__VLS_StyleScopedClasses['svc-meta']} */ ;
/** @type {__VLS_StyleScopedClasses['svc-code']} */ ;
/** @type {__VLS_StyleScopedClasses['right']} */ ;
/** @type {__VLS_StyleScopedClasses['cart']} */ ;
/** @type {__VLS_StyleScopedClasses['header-row']} */ ;
/** @type {__VLS_StyleScopedClasses['card-title']} */ ;
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
/** @type {__VLS_StyleScopedClasses['hint']} */ ;
// @ts-ignore
var __VLS_13 = __VLS_12, __VLS_57 = __VLS_56;
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
            serviceFilterInput: serviceFilterInput,
            memberInput: memberInput,
            quickAddFirst: quickAddFirst,
            availableRooms: availableRooms,
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
