<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar" role="search">
        <el-checkbox v-model="query.includeClosed" @change="reload">显示已关闭会员</el-checkbox>
        <el-input
          v-model="query.keyword"
          placeholder="卡号 / 手机号 / 姓名"
          clearable
          style="width: 380px"
          aria-label="搜索会员，输入卡号、手机号或姓名后回车"
          @keyup.enter="reload"
        />
        <el-button type="primary" aria-label="执行会员搜索" @click="reload">查询</el-button>
        <el-button aria-label="重置搜索条件" @click="resetQuery">重置</el-button>
        <el-button type="primary" aria-label="开新会员卡" @click="openCreate">开卡</el-button>
      </div>

      <div
        v-loading="loading"
        class="member-list"
        role="list"
        aria-label="会员列表，按手机号聚合，每个会员可展开旗下所有卡"
      >
        <div v-if="!loading && groups.length === 0" class="empty-tip" role="status">
          暂无会员，使用上方"开卡"按钮新开第一张
        </div>

        <article
          v-for="g in groups"
          :key="g.phone"
          class="member-card"
          role="listitem"
          :aria-label="groupAriaLabel(g)"
        >
          <header class="member-head">
            <div class="head-main">
              <div class="head-line1">
                <span class="phone">{{ g.phone }}</span>
                <span class="name">{{ g.primaryName || '未填姓名' }}</span>
                <span v-if="g.hasInactive" class="badge badge-muted" aria-label="该会员名下含已关闭的卡">含关闭卡</span>
              </div>
              <div class="head-stats" role="group" :aria-label="`${g.primaryName || g.phone} 的资金汇总`">
                <span class="stat">
                  <span class="stat-label">持卡</span>
                  <strong class="stat-val">{{ g.cardCount }} 张</strong>
                </span>
                <span class="stat">
                  <span class="stat-label">总余额</span>
                  <strong class="stat-val money" :aria-label="`总余额 ${yuanReadable(g.totalBalance)}`">
                    ¥{{ g.totalBalance.toFixed(2) }}
                  </strong>
                </span>
                <span class="stat">
                  <span class="stat-label">累计充值</span>
                  <strong class="stat-val" :aria-label="`累计充值 ${yuanReadable(g.totalRecharge)}`">
                    ¥{{ g.totalRecharge.toFixed(2) }}
                  </strong>
                </span>
                <span class="stat">
                  <span class="stat-label">累计消费</span>
                  <strong class="stat-val" :aria-label="`累计消费 ${yuanReadable(g.totalConsumed)}`">
                    ¥{{ g.totalConsumed.toFixed(2) }}
                  </strong>
                </span>
              </div>
            </div>
            <div class="head-actions">
              <el-button
                size="small"
                :aria-expanded="isExpanded(g.phone)"
                :aria-controls="`cards-${g.phone}`"
                :aria-label="`${isExpanded(g.phone) ? '收起' : '展开'} ${g.primaryName || g.phone} 名下 ${g.cardCount} 张卡的卡号与明细`"
                @click="toggleExpand(g.phone)"
              >
                {{ isExpanded(g.phone) ? `收起卡号（${g.cardCount} 张）` : `展开卡号（${g.cardCount} 张）` }}
              </el-button>
              <el-button
                type="success"
                size="small"
                :aria-label="`为 ${g.primaryName || g.phone} 加办一张新会员卡`"
                @click="openCreateForPhone(g.phone)"
              >
                加办一张卡
              </el-button>
            </div>
          </header>

          <ul
            v-if="isExpanded(g.phone)"
            :id="`cards-${g.phone}`"
            class="card-list"
            role="list"
            :aria-label="`${g.primaryName || g.phone} 名下卡列表，共 ${g.cardCount} 张`"
          >
            <li
              v-for="c in g.cards"
              :key="c.id"
              class="card-row"
              :class="{ closed: !c.isActive }"
              :aria-label="cardAriaLabel(c, g.primaryName)"
            >
              <div class="card-info">
                <div class="card-line1">
                  <span class="card-no">卡号 {{ c.cardNo }}</span>
                  <span
                    v-if="c.memberTypeName"
                    class="badge"
                    :class="c.memberTypeKind === 'StoredValue' ? 'badge-warning' : 'badge-success'"
                    :aria-label="`卡类型 ${c.memberTypeName}`"
                  >
                    {{ c.memberTypeName }}
                  </span>
                  <span v-if="c.discount < 1" class="badge badge-warning" :aria-label="`折扣 ${(c.discount * 10).toFixed(1)} 折`">
                    {{ (c.discount * 10).toFixed(1) }} 折
                  </span>
                  <span v-if="!c.isActive" class="badge badge-info" aria-label="该卡已关闭">已关闭</span>
                </div>
                <div class="card-stats" role="group" :aria-label="`卡 ${c.cardNo} 的资金明细`">
                  <span class="stat">
                    <span class="stat-label">余额</span>
                    <strong class="stat-val money" :aria-label="`余额 ${yuanReadable(c.balance)}`">
                      ¥{{ c.balance.toFixed(2) }}
                    </strong>
                  </span>
                  <span class="stat">
                    <span class="stat-label">充值</span>
                    <strong class="stat-val" :aria-label="`累计充值 ${yuanReadable(c.totalRecharge)}`">
                      ¥{{ c.totalRecharge.toFixed(2) }}
                    </strong>
                  </span>
                  <span class="stat">
                    <span class="stat-label">消费</span>
                    <strong class="stat-val" :aria-label="`累计消费 ${yuanReadable(c.totalConsumed)}`">
                      ¥{{ c.totalConsumed.toFixed(2) }}
                    </strong>
                  </span>
                  <span v-if="c.memberTypeKind === 'CountBased'" class="stat">
                    <span class="stat-label">次数</span>
                    <strong class="stat-val" :aria-label="`累计 ${c.totalCount ?? 0} 次，剩 ${c.remainCount ?? 0} 次`">
                      {{ c.totalCount ?? 0 }} / 剩 {{ c.remainCount ?? 0 }} 次
                    </strong>
                  </span>
                </div>
                <div class="card-validity" :aria-label="`开卡时间 ${cardStartText(c)}，${cardValidityAria(c)}`">
                  <span class="stat-label">开卡</span>
                  <span class="validity-val">{{ cardStartText(c) }}</span>
                  <span class="validity-val" style="margin-left:14px" :class="{ expired: (c.cardDaysRemaining ?? 1) < 0 }">{{ cardValidityText(c) }}</span>
                </div>
              </div>
              <div class="card-actions" role="group" :aria-label="`卡 ${c.cardNo} 操作`">
                <el-button
                  type="primary"
                  size="small"
                  :disabled="!c.isActive"
                  :aria-label="`为卡 ${c.cardNo} 充值，当前余额 ${yuanReadable(c.balance)}`"
                  @click="openRecharge(c)"
                >充值</el-button>
                <el-button
                  size="small"
                  :aria-label="`查看卡 ${c.cardNo} 的充值与消费流水`"
                  @click="openHistory(c)"
                >流水</el-button>
                <el-button
                  size="small"
                  :aria-label="`编辑卡 ${c.cardNo} 的会员资料`"
                  @click="openEdit(c)"
                >编辑</el-button>
                <el-button
                  type="warning"
                  size="small"
                  :disabled="!c.isActive || c.balance <= 0"
                  :aria-label="`退卡 ${c.cardNo}，把余额 ${yuanReadable(c.balance)} 退还给客户`"
                  @click="openRefund(c)"
                >退卡</el-button>
                <el-button
                  type="warning"
                  size="small"
                  :disabled="!c.isActive || c.balance <= 0"
                  :aria-label="`将卡 ${c.cardNo} 的余额转赠给其他会员`"
                  @click="openTransfer(c)"
                >转赠</el-button>
                <el-button
                  size="small"
                  :aria-label="`查看卡 ${c.cardNo} 的引荐记录`"
                  @click="openReferrals(c)"
                >引荐</el-button>
              </div>
            </li>
          </ul>
        </article>
      </div>

      <el-pagination
        style="margin-top: 12px; justify-content: flex-end; display: flex"
        :current-page="query.page"
        :page-size="query.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="(p: number) => { query.page = p; reload(); }"
        @size-change="(s: number) => { query.pageSize = s; query.page = 1; reload(); }"
      />
    </el-card>

    <el-dialog
      v-model="formOpen"
      :title="formMode === 'create' ? '开卡' : '编辑会员'"
      :width="formMode === 'create' ? '900px' : '560px'"
      top="6vh"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="92px" class="member-form">
        <el-row :gutter="16">
          <el-col :span="formMode === 'create' ? 12 : 24">
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="form.phone" :disabled="formMode === 'create' && phoneLocked" />
            </el-form-item>
          </el-col>
          <el-col :span="formMode === 'create' ? 12 : 24">
            <el-form-item label="卡号" prop="cardNo">
              <el-input v-model="form.cardNo" :disabled="formMode === 'edit'" />
            </el-form-item>
          </el-col>
        </el-row>
        <div v-if="phoneLocked && formMode === 'create'" class="muted lock-tip">为该客户加办新卡，手机号已锁定</div>

        <el-row :gutter="16">
          <el-col :span="formMode === 'create' ? 12 : 24">
            <el-form-item label="姓名">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>
          <el-col :span="formMode === 'create' ? 12 : 24">
            <el-form-item label="性别">
              <el-radio-group v-model="form.gender">
                <el-radio value="男">男</el-radio>
                <el-radio value="女">女</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="formMode === 'create' ? 12 : 24">
            <el-form-item label="生日">
              <el-date-picker v-model="form.birthday" type="date" placeholder="可选" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col v-if="formMode === 'create'" :span="12">
            <el-form-item label="会员类型" prop="memberTypeId" required>
              <el-select
                v-model="form.memberTypeId"
                placeholder="开卡必选一个会员类型"
                style="width: 100%"
                :loading="typesLoading"
                @change="onCreateTypePicked"
              >
                <el-option
                  v-for="t in activeMemberTypes"
                  :key="t.id"
                  :value="t.id"
                  :label="t.name"
                >
                  <span>{{ t.name }}</span>
                  <span class="opt-meta">
                    {{ t.kind === 'StoredValue' ? `充值卡 · 最低¥${t.minRechargeAmount?.toFixed(0)}` : `计次卡 · ${t.serviceItemName ?? ''} · 最少${t.minPurchaseCount}次` }}
                    {{ t.discount < 1 ? ' · ' + (t.discount * 10).toFixed(1) + '折' : '' }}
                  </span>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item v-if="formMode === 'create' && selectedCreateType" label="类型规则" class="full-row">
          <el-descriptions :column="3" size="small" border class="type-summary">
            <el-descriptions-item label="类型">
              <el-tag size="small" :type="selectedCreateType.kind === 'StoredValue' ? 'warning' : 'success'">
                {{ selectedCreateType.kind === 'StoredValue' ? '充值卡' : '计次卡' }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="折扣">
              <span v-if="selectedCreateType.discount < 1">{{ (selectedCreateType.discount * 10).toFixed(1) }} 折</span>
              <span v-else>原价</span>
            </el-descriptions-item>
            <el-descriptions-item label="最低门槛">
              <span v-if="selectedCreateType.kind === 'StoredValue'">¥{{ selectedCreateType.minRechargeAmount?.toFixed(2) }}</span>
              <span v-else>{{ selectedCreateType.minPurchaseCount }} 次</span>
            </el-descriptions-item>
            <el-descriptions-item label="赠送">
              <span v-if="selectedCreateType.kind === 'StoredValue' && (selectedCreateType.bonusAmount ?? 0) > 0">
                ¥{{ selectedCreateType.bonusAmount?.toFixed(2) }}
              </span>
              <span v-else-if="selectedCreateType.kind === 'CountBased' && (selectedCreateType.bonusCount ?? 0) > 0">
                {{ selectedCreateType.bonusCount }} 次
              </span>
              <span v-else class="muted">无</span>
            </el-descriptions-item>
            <el-descriptions-item label="到期日" :span="2">
              <strong v-if="createCardExpireText" :class="{ permanent: createCardExpireText === '永久' }">
                {{ createCardExpireText }}
              </strong>
            </el-descriptions-item>
            <el-descriptions-item v-if="selectedCreateType.kind === 'CountBased' && selectedCreateType.serviceItemName" label="绑定服务" :span="3">
              {{ selectedCreateType.serviceItemName }}
            </el-descriptions-item>
          </el-descriptions>
        </el-form-item>

        <el-row v-if="formMode === 'create' && selectedCreateType?.kind === 'StoredValue'" :gutter="16">
          <el-col :span="12">
            <el-form-item label="充值金额" prop="initialBalance" required>
              <el-input-number
                v-model="form.initialBalance"
                :min="selectedCreateType.minRechargeAmount ?? 0.01"
                :precision="2"
                :step="100"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实收金额">
              <strong>¥{{ chargeAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left: 8px">× {{ (form.discount * 10).toFixed(1) }} 折</span>
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="实充金额">
              <strong>¥{{ creditAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left: 10px">
                充值金额 + 赠送 ¥{{ (selectedCreateType.bonusAmount ?? 0).toFixed(2) }} = 卡内余额
              </span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row v-if="formMode === 'create' && selectedCreateType?.kind === 'CountBased'" :gutter="16">
          <el-col :span="12">
            <el-form-item label="购买次数" prop="count" required>
              <el-input-number
                v-model="form.count"
                :min="selectedCreateType.minPurchaseCount ?? 1"
                :step="1"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实充次数">
              <strong>{{ creditCount }} 次</strong>
              <span class="muted" style="margin-left: 8px">含赠送 {{ selectedCreateType.bonusCount ?? 0 }} 次</span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="充值金额">
              <strong>¥{{ countFaceAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left: 8px">
                <template v-if="boundService">{{ form.count }} 次 × ¥{{ boundUnitPrice.toFixed(2) }}</template>
                <template v-else>未配置会员价</template>
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实收金额">
              <strong>¥{{ countChargeAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left: 8px">× {{ (form.discount * 10).toFixed(1) }} 折</span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row v-if="formMode === 'create'" :gutter="16">
          <el-col :span="16">
            <el-form-item label="支付来源" prop="payMethod" required>
              <el-radio-group v-model="form.payMethod">
                <el-radio-button value="Wechat">微信</el-radio-button>
                <el-radio-button value="Alipay">支付宝</el-radio-button>
                <el-radio-button value="BankCard">银行卡</el-radio-button>
                <el-radio-button value="Cash">现金</el-radio-button>
                <el-radio-button value="Other">其它</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="折扣" prop="discount">
              <el-input-number
                v-model="form.discount"
                :min="0.1"
                :max="1"
                :step="0.05"
                :precision="2"
                disabled
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="员工推荐人" class="full-row">
          <el-select
            v-model="form.referredByStaffId"
            clearable
            filterable
            :placeholder="formMode === 'create'
              ? '本店员工，可选（开卡给该员工记一笔推荐提成）'
              : '本店员工，可选（改派/清除会同步调整推荐提成）'"
            style="width: 100%"
          >
            <el-option
              v-for="s in staffOptions"
              :key="s.id"
              :value="s.id"
              :label="`${s.employeeNo ?? ''} · ${s.realName ?? s.username}`"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="顾客引荐人" class="full-row">
          <el-select
            :model-value="form.referrerPhone"
            filterable
            remote
            clearable
            :remote-method="searchReferrer"
            :loading="referrerSearchLoading"
            placeholder="输入卡号或手机号搜索，按人聚合，选中即可"
            style="width: 100%"
            @update:model-value="onReferrerSelected"
          >
            <!-- 编辑时带出的当前引荐人占位项（未重新搜索前显示其姓名） -->
            <el-option
              v-if="form.referrerPhone === editReferrerSentinel"
              :value="editReferrerSentinel"
              :label="form.referrerLabel || '当前引荐人'"
            />
            <el-option
              v-for="g in referrerSearchResults"
              :key="g.phone"
              :value="g.phone"
              :label="referrerOptionLabel(g)"
            />
          </el-select>
          <div class="muted" style="margin-top:4px">
            按手机号聚合显示，一个会员只出一行（含名下所有卡）。已选：
            <strong v-if="form.referrerLabel">{{ form.referrerLabel }}</strong>
            <span v-else>无</span>
            <el-button
              v-if="form.referredByMemberId"
              link
              type="danger"
              size="small"
              style="margin-left:6px"
              @click="clearReferrer"
            >清除</el-button>
          </div>
        </el-form-item>

        <el-form-item v-if="formMode === 'create' && hasReferralPreview" label-width="0" class="full-row">
          <div class="referral-preview" aria-label="推荐奖励预估">
            <div class="rp-title">开卡后将产生的推荐奖励（按当前推荐规则预估）</div>
            <div v-if="customerRewardPreview" class="rp-line">{{ customerRewardPreview }}</div>
            <div v-if="staffRewardPreview" class="rp-line">{{ staffRewardPreview }}</div>
          </div>
        </el-form-item>

        <el-form-item label="备注" class="full-row">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveForm">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="rechargeOpen" :title="`充值：${rechargeTarget?.cardNo}`" width="460px">
      <el-form :model="rcForm" label-width="100px" v-loading="typesLoading">
        <el-form-item label="当前余额">¥ {{ rechargeTarget?.balance.toFixed(2) ?? '0.00' }}</el-form-item>

        <el-alert v-if="rechargeType" type="info" :closable="false" show-icon style="margin-bottom:12px">
          <template #title>
            <div>{{ rechargeType.name }}（{{ rechargeType.kind === 'StoredValue' ? '充值卡' : '计次卡' }}）</div>
            <div style="font-size:12px; margin-top:4px">
              <span v-if="rechargeType.kind === 'StoredValue'">最低充值 ¥{{ rechargeType.minRechargeAmount?.toFixed(2) }}</span>
              <span v-else>最少 {{ rechargeType.minPurchaseCount }} 次（绑定：{{ rechargeType.serviceItemName }}）</span>
              <span v-if="rechargeType.discount < 1"> · 折扣 {{ (rechargeType.discount * 10).toFixed(1) }} 折</span>
              <span v-if="rechargeType.kind === 'StoredValue' && (rechargeType.bonusAmount ?? 0) > 0"> · 送 ¥{{ rechargeType.bonusAmount?.toFixed(2) }}</span>
              <span v-if="rechargeType.kind === 'CountBased' && (rechargeType.bonusCount ?? 0) > 0"> · 送 {{ rechargeType.bonusCount }} 次</span>
            </div>
          </template>
        </el-alert>

        <template v-if="rechargeType?.kind === 'CountBased'">
          <el-form-item label="充值次数">
            <el-input-number
              v-model="rcForm.count"
              :min="rechargeType.minPurchaseCount ?? 1"
              :step="1"
            />
            <span class="muted" style="margin-left:8px">
              最低 {{ rechargeType.minPurchaseCount }} 次
              <span v-if="(rechargeType.bonusCount ?? 0) > 0"> · 赠送 {{ rechargeType.bonusCount }} 次</span>
            </span>
          </el-form-item>
          <el-form-item label="实充次数">
            <strong>{{ rechargeCreditCount }} 次</strong>
            <span class="muted" style="margin-left:8px">充值次数 + 赠送 {{ rechargeType.bonusCount ?? 0 }} 次 = 卡内可用次数</span>
          </el-form-item>
          <el-form-item label="充值金额">
            <strong>¥{{ rechargeCountFaceAmount.toFixed(2) }}</strong>
            <span class="muted" style="margin-left:8px">
              <template v-if="rechargeBoundService">
                {{ rcForm.count }} 次 × 会员价 ¥{{ rechargeBoundUnitPrice.toFixed(2) }}（{{ rechargeBoundService.name }}）
              </template>
              <template v-else>绑定服务未配置会员价</template>
            </span>
          </el-form-item>
          <el-form-item label="实收金额">
            <strong>¥{{ rechargeCountChargeAmount.toFixed(2) }}</strong>
            <span class="muted" style="margin-left:8px">
              充值金额 × {{ ((rechargeType.discount ?? 1) * 10).toFixed(1) }} 折 = 客户实付
            </span>
          </el-form-item>
        </template>

        <template v-else>
          <el-form-item label="充值金额">
            <el-input-number
              v-model="rcForm.amount"
              :min="rechargeType?.minRechargeAmount ?? 0"
              :precision="2"
              :step="100"
            />
            <span v-if="rechargeType" class="muted" style="margin-left:8px">
              最低 ¥{{ rechargeType.minRechargeAmount?.toFixed(2) }}
            </span>
          </el-form-item>
          <el-form-item label="赠送金额">
            <el-input-number
              v-model="rcForm.bonusAmount"
              :min="0"
              :precision="2"
              :step="50"
              :disabled="!!rechargeType"
            />
            <span v-if="rechargeType" class="muted" style="margin-left:8px">由会员类型决定</span>
          </el-form-item>
          <template v-if="rechargeType">
            <el-form-item label="实收金额">
              <strong>¥{{ rechargeChargeAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left:8px">
                充值金额 × {{ ((rechargeType.discount ?? 1) * 10).toFixed(1) }} 折 = 客户实付
              </span>
            </el-form-item>
            <el-form-item label="实充金额">
              <strong>¥{{ rechargeCreditAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left:8px">
                充值金额 + 赠送 ¥{{ (rechargeType.bonusAmount ?? 0).toFixed(2) }} = 卡内余额
              </span>
            </el-form-item>
          </template>
        </template>

        <el-form-item label="支付方式">
          <el-radio-group v-model="rcForm.payMethod">
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Other">其它</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="rcForm.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rechargeOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="doRecharge">确认充值</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="issueOpen" :title="`办卡：${issueTarget?.cardNo}`" width="480px">
      <el-form :model="issueForm" label-width="100px" v-loading="typesLoading">
        <el-form-item label="选择类型">
          <el-select v-model="issueForm.memberTypeId" placeholder="选一个会员类型" style="width:100%" @change="onIssueTypeChange">
            <el-option
              v-for="t in activeMemberTypes"
              :key="t.id"
              :value="t.id"
              :label="t.name"
            >
              <span>{{ t.name }}</span>
              <span class="opt-meta">
                {{ t.kind === 'StoredValue' ? `充值卡 · 最低¥${t.minRechargeAmount?.toFixed(0)}` : `计次卡 · ${t.serviceItemName ?? ''} · 最少${t.minPurchaseCount}次` }}
                {{ t.discount < 1 ? ' · ' + (t.discount * 10).toFixed(1) + '折' : '' }}
              </span>
            </el-option>
          </el-select>
        </el-form-item>
        <template v-if="selectedIssueType">
          <el-alert type="info" :closable="false" show-icon style="margin-bottom:12px">
            <template #title>
              <div>{{ selectedIssueType.name }}</div>
              <div style="font-size:12px; margin-top:4px">
                <span v-if="selectedIssueType.kind === 'StoredValue'">最低充值 ¥{{ selectedIssueType.minRechargeAmount?.toFixed(2) }}</span>
                <span v-else>最少 {{ selectedIssueType.minPurchaseCount }} 次（绑定：{{ selectedIssueType.serviceItemName }}）</span>
                <span v-if="selectedIssueType.discount < 1"> · 折扣 {{ (selectedIssueType.discount * 10).toFixed(1) }} 折</span>
                <span v-if="selectedIssueType.kind === 'StoredValue' && (selectedIssueType.bonusAmount ?? 0) > 0"> · 送 ¥{{ selectedIssueType.bonusAmount?.toFixed(2) }}</span>
                <span v-if="selectedIssueType.kind === 'CountBased' && (selectedIssueType.bonusCount ?? 0) > 0"> · 送 {{ selectedIssueType.bonusCount }} 次</span>
                <span v-if="selectedIssueType.validDays"> · 有效 {{ selectedIssueType.validDays }} 天</span>
                <span v-else> · 永久有效</span>
              </div>
            </template>
          </el-alert>
          <el-form-item v-if="selectedIssueType.kind === 'StoredValue'" label="充值金额">
            <el-input-number
              v-model="issueForm.amount"
              :min="selectedIssueType.minRechargeAmount ?? 1"
              :precision="2" :step="100"
            />
          </el-form-item>
          <el-form-item v-else label="购买次数">
            <el-input-number
              v-model="issueForm.count"
              :min="selectedIssueType.minPurchaseCount ?? 1"
              :step="1"
            />
            <el-input-number
              v-model="issueForm.amount"
              :min="0" :precision="2" :step="100"
              style="margin-left:12px"
            />
            <span class="muted" style="margin-left:6px">实收金额</span>
          </el-form-item>
        </template>
        <el-form-item label="支付方式">
          <el-radio-group v-model="issueForm.payMethod">
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Other">其它</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="issueForm.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="issueOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" :disabled="!selectedIssueType" @click="doIssueCard">确认办卡</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="historyOpen" title="会员流水" size="880px">
      <el-tabs v-if="historyTarget" v-model="historyTab">
        <el-tab-pane label="资金流水" name="recharge">
          <el-table :data="rechargeList" size="small">
            <el-table-column label="类型" width="90">
              <template #default="{ row }">
                <el-tag size="small" :type="rechargeKindTag(row.kind)">{{ rechargeKindLabel(row.kind) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="amount" label="金额" width="100">
              <template #default="{ row }">¥{{ row.amount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="bonusAmount" label="赠送" width="80">
              <template #default="{ row }">¥{{ row.bonusAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="balanceAfter" label="后余额" width="100">
              <template #default="{ row }">¥{{ row.balanceAfter.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="payMethod" label="支付" width="80">
              <template #default="{ row }">{{ payMethodLabel(row.payMethod) }}</template>
            </el-table-column>
            <el-table-column label="对手会员" width="120">
              <template #default="{ row }">{{ row.counterpartyMemberName || '—' }}</template>
            </el-table-column>
            <el-table-column label="时间" min-width="140">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm:ss') }}</template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="消费记录" name="consume">
          <el-table :data="orderList" size="small">
            <el-table-column prop="orderNo" label="订单" min-width="160" />
            <el-table-column prop="paidAmount" label="实收" width="100">
              <template #default="{ row }">¥{{ row.paidAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="80">
              <template #default="{ row }">{{ orderStatusLabel(row.status) }}</template>
            </el-table-column>
            <el-table-column label="时间" min-width="140">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm:ss') }}</template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-drawer>

    <el-dialog v-model="refundOpen" :title="`退卡：${refundTarget?.cardNo}`" width="420px">
      <el-form :model="rfForm" label-width="100px">
        <el-form-item label="当前余额">
          <strong style="color: #d9534f">¥ {{ refundTarget?.balance.toFixed(2) ?? '0.00' }}</strong>
        </el-form-item>
        <el-form-item label="退款金额" required>
          <el-input-number v-model="rfForm.refundAmount" :min="0.01" :max="refundTarget?.balance ?? 0" :precision="2" :step="50" />
          <el-button link size="small" style="margin-left:8px" @click="rfForm.refundAmount = refundTarget?.balance ?? 0">全退</el-button>
        </el-form-item>
        <el-form-item label="退款方式" required>
          <el-radio-group v-model="rfForm.refundMethod">
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Other">其它</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="原因">
          <el-input v-model="rfForm.reason" type="textarea" :rows="2" maxlength="200" />
        </el-form-item>
        <el-alert type="warning" :closable="false" title="退卡后会员卡将被关闭，不能再充值或消费。请确认与客户达成一致再操作。" />
      </el-form>
      <template #footer>
        <el-button @click="refundOpen = false">取消</el-button>
        <el-button type="warning" :loading="saving" @click="doRefund">确认退卡</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="transferOpen" :title="`转赠：${transferTarget?.cardNo}`" width="460px">
      <el-form :model="tfForm" label-width="100px">
        <el-form-item label="当前余额">
          <strong style="color: #d9534f">¥ {{ transferTarget?.balance.toFixed(2) ?? '0.00' }}</strong>
          <span class="muted" style="margin-left:8px">将一并转走</span>
        </el-form-item>
        <el-form-item label="转赠对象">
          <el-radio-group v-model="tfForm.mode">
            <el-radio-button value="existing">已有会员</el-radio-button>
            <el-radio-button value="new">新建会员</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <template v-if="tfForm.mode === 'existing'">
          <el-form-item label="目标会员">
            <el-input v-model="tfForm.toQuery" placeholder="输入卡号 / 手机号搜索" @keyup.enter="searchTarget" />
            <el-button link size="small" @click="searchTarget">查找</el-button>
            <el-radio-group v-if="targetCandidates.length" v-model="tfForm.toMemberId" style="margin-top:8px; display:flex; flex-direction:column; gap:6px">
              <el-radio v-for="c in targetCandidates" :key="c.id" :value="c.id">
                {{ c.cardNo }}（{{ c.name || '未填' }} / {{ c.phone }}） 余额 ¥{{ c.balance.toFixed(2) }}
              </el-radio>
            </el-radio-group>
          </el-form-item>
        </template>
        <template v-else>
          <el-form-item label="新会员卡号" required>
            <el-input v-model="tfForm.newMemberCardNo" />
          </el-form-item>
          <el-form-item label="手机号" required>
            <el-input v-model="tfForm.newMemberPhone" />
          </el-form-item>
          <el-form-item label="姓名">
            <el-input v-model="tfForm.newMemberName" />
          </el-form-item>
        </template>
        <el-form-item label="原因">
          <el-input v-model="tfForm.reason" type="textarea" :rows="2" maxlength="200" placeholder="如：转赠给家人 / 卡号变更" />
        </el-form-item>
        <el-alert type="warning" :closable="false" title="转赠后原卡余额清零并关闭，目标卡余额累加（不计入对方累计充值，不会因此升级）。" />
      </el-form>
      <template #footer>
        <el-button @click="transferOpen = false">取消</el-button>
        <el-button type="warning" :loading="saving" @click="doTransfer">确认转赠</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="referralsOpen" :title="`${referralsData?.referrerName} 的引荐情况`" width="640px">
      <div v-if="referralsData">
        <div class="metric-row">
          <div>已引荐 <strong>{{ referralsData.referredCount }}</strong> 人</div>
          <div>累计返佣 <strong style="color:#d9534f">¥{{ referralsData.totalRewardEarned.toFixed(2) }}</strong></div>
        </div>
        <el-table :data="referralsData.referredMembers" size="small" stripe style="margin-top:12px">
          <el-table-column prop="cardNo" label="卡号" width="120" />
          <el-table-column prop="name" label="姓名" width="100" />
          <el-table-column prop="phone" label="手机号" width="140" />
          <el-table-column label="累计充值" width="120">
            <template #default="{ row }">¥{{ row.totalRecharge.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column label="开卡时间" min-width="160">
            <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm:ss') }}</template>
          </el-table-column>
        </el-table>
        <p v-if="referralsData.referredCount === 0" class="muted" style="text-align:center; padding:30px 0">还没有引荐过会员</p>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import dayjs from 'dayjs';
import { memberTypesApi, membersApi, referralSettingsApi, servicesApi, staffApi, type MemberType, type ReferralSetting, type ReferralSummaryDto } from '@/api/modules';
import { orderStatusLabel, payMethodLabel } from '@/utils/enumLabels';
import { useAppStore } from '@/stores/app';
import type { Member, MemberPhoneGroup, ServiceItem, Staff } from '@/api/types';

const appStore = useAppStore();

const groups = ref<MemberPhoneGroup[]>([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);

const query = reactive({ page: 1, pageSize: 20, keyword: '', includeClosed: false });

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);

const formRef = ref<FormInstance>();
const form = reactive({
  cardNo: '',
  phone: '',
  name: '',
  gender: '',
  birthday: '',
  discount: 1,
  initialBalance: 0,
  remark: '',
  // 引荐人按"人"维度选：select 的 v-model 用 phone 唯一定位一个人，
  // referredByMemberId 仍按现行 schema 传一张卡的 Id（取该人首张可用卡）以满足后端 FK
  referrerPhone: '',
  referrerLabel: '',
  referredByMemberId: null as number | null,
  referredByStaffId: null as number | null,
  memberTypeId: null as number | null,
  count: 0,
  payMethod: 'Wechat'
});
const referrerSearchResults = ref<MemberPhoneGroup[]>([]);
const referrerSearchLoading = ref(false);
// 编辑时把已有引荐人作为"当前项"塞进下拉的占位 value（手机号未知，用哨兵值占位，让控件能显示并可清除）
const editReferrerSentinel = '__current_referrer__';

// 员工推荐人候选 + 推荐规则（用于开卡时预估顾客返佣 / 员工提成）
const staffOptions = ref<Staff[]>([]);
const referral = ref<ReferralSetting | null>(null);
const round2 = (n: number) => Math.round(n * 100) / 100;
async function ensureReferralContextLoaded() {
  // 员工列表按当前门店每次加载（切店后保持正确）；推荐规则全店统一，仅首次加载
  try {
    const r = await staffApi.list({ pageSize: 200, storeId: appStore.activeStoreId ?? undefined });
    staffOptions.value = r.items.filter((s) => s.isActive);
  } catch { /* 非致命：员工推荐人列表加载失败不影响开卡 */ }
  if (!referral.value) {
    try { referral.value = await referralSettingsApi.get(); } catch { /* 非致命 */ }
  }
}

/// 算下一张卡的卡号：N=同手机号下已有卡数（含已关闭），统一两位后缀
///   N=0 → phone+01
///   N=k → phone+(k+1).padStart(2,'0')
async function computeNextCardNo(phone: string): Promise<string> {
  if (!phone) return phone;
  // 优先使用列表里已加载的 groups 数据，省一次请求
  const localGroup = groups.value.find((g) => g.phone === phone);
  if (localGroup) {
    return phone + String(localGroup.cardCount + 1).padStart(2, '0');
  }
  // 列表没找到（用户手输了一个新手机号）— 11 位时才查询
  if (phone.length !== 11) return phone;
  try {
    const r = await membersApi.list({ keyword: phone, pageSize: 100, includeClosed: true });
    const n = r.items.filter((m) => m.phone === phone).length;
    return phone + String(n + 1).padStart(2, '0');
  } catch {
    return phone;
  }
}

/// 开卡场景下，手机号变动时自动同步到卡号：
/// - 第 1 张：cardNo = phone（与手机号一致）
/// - 第 2 张起：cardNo = phone + NN（02、03 …，便于一人多卡）
/// 用户仍可在卡号输入框里手动覆盖，覆盖后该次开卡不再被 phone 变更覆盖。
watch(() => form.phone, async (newPhone) => {
  if (formMode.value !== 'create') return;
  // 未到 11 位先做明文同步，给用户一边输入一边看反馈
  if (newPhone.length < 11) {
    form.cardNo = newPhone;
    return;
  }
  const snapshot = newPhone;
  const next = await computeNextCardNo(newPhone);
  // 若期间手机号又改了，丢弃过期结果
  if (form.phone === snapshot) form.cardNo = next;
});

// ---- 会员类型相关（共享给 创建对话框 + 办卡对话框）----
const memberTypes = ref<MemberType[]>([]);
const typesLoading = ref(false);
const services = ref<ServiceItem[]>([]);
const activeMemberTypes = computed(() => memberTypes.value.filter((t) => t.isActive));

async function ensureMemberTypesLoaded() {
  if (memberTypes.value.length > 0) return;
  typesLoading.value = true;
  try {
    const [ts, svc] = await Promise.all([
      memberTypesApi.list(false).catch(() => [] as MemberType[]),
      services.value.length ? Promise.resolve(services.value) : servicesApi.list(false).catch(() => [] as ServiceItem[])
    ]);
    memberTypes.value = ts;
    services.value = svc;
  } finally {
    typesLoading.value = false;
  }
}

/// 计次卡绑定的服务项目（含会员价）
const boundService = computed<ServiceItem | null>(() => {
  const t = selectedCreateType.value;
  if (!t || t.kind !== 'CountBased' || !t.serviceItemId) return null;
  return services.value.find((s) => s.id === t.serviceItemId) ?? null;
});

/// 服务单价：优先取会员价；若为 0 则回退到标准价以便看到合理数值
const boundUnitPrice = computed(() => {
  const s = boundService.value;
  if (!s) return 0;
  return s.memberPrice > 0 ? s.memberPrice : s.price;
});

/// 计次卡：充值金额 = 次数 × 绑定服务会员价
const countFaceAmount = computed(() =>
  Math.round(form.count * boundUnitPrice.value * 100) / 100
);

/// 计次卡：实收金额 = 充值金额 × 折扣
const countChargeAmount = computed(() =>
  Math.round(countFaceAmount.value * form.discount * 100) / 100
);

const selectedCreateType = computed<MemberType | null>(() =>
  memberTypes.value.find((t) => t.id === form.memberTypeId) ?? null);

/// 到期日展示：ValidDays 空 / ≤0 → 永久；否则今天 + N 天
const createCardExpireText = computed(() => {
  const t = selectedCreateType.value;
  if (!t) return '';
  if (!t.validDays || t.validDays <= 0) return '永久';
  return dayjs().add(t.validDays, 'day').format('YYYY-MM-DD') + `（${t.validDays} 天后）`;
});

/// 充值卡：实收金额 = 充值金额 × 折扣（客户实际刷卡/现金）
const chargeAmount = computed(() =>
  Math.round(form.initialBalance * form.discount * 100) / 100
);

/// 充值卡：实充金额 = 充值金额 + 赠送（卡内余额）
const creditAmount = computed(() => {
  const bonus = selectedCreateType.value?.bonusAmount ?? 0;
  return Math.round((form.initialBalance + bonus) * 100) / 100;
});

/// 计次卡：实充次数 = 购买次数 + 赠送次数（卡内可用次数）
const creditCount = computed(() => {
  const bonus = selectedCreateType.value?.bonusCount ?? 0;
  return form.count + bonus;
});

/// 开卡实收基数（与提交时 paidAmount 一致）：计次卡=实收金额，充值卡=充值面值
const currentPaid = computed(() =>
  selectedCreateType.value?.kind === 'CountBased' ? countChargeAmount.value : form.initialBalance);

/// 顾客推荐返佣预估（按当前推荐规则；返佣方式二选一/关闭）
const customerRewardPreview = computed<string | null>(() => {
  const r = referral.value;
  if (!form.referredByMemberId || !r) return null;
  let amt = 0; let desc = '';
  if (r.customerReferralMode === 'PercentPerRecharge') { amt = round2(currentPaid.value * r.customerRewardPercent / 100); desc = `充值返佣 ${r.customerRewardPercent}%`; }
  else if (r.customerReferralMode === 'FixedPerCard') { amt = r.customerFixedReward; desc = '固定推荐费 / 张'; }
  return amt > 0
    ? `顾客推荐人：返佣 ¥${amt.toFixed(2)}（${desc}）→ 进推荐顾客余额`
    : '顾客推荐人：暂无返佣（推荐规则未开启或额度为 0）';
});

/// 员工推荐提成预估（按当前推荐规则；固定/百分比二选一/关闭）
const staffRewardPreview = computed<string | null>(() => {
  const r = referral.value;
  if (!form.referredByStaffId || !r) return null;
  let amt = 0; let desc = '';
  if (r.staffReferralMode === 'FixedPerCard') { amt = r.staffReferralFixedAmount; desc = '固定提成 / 张'; }
  else if (r.staffReferralMode === 'PercentOfOpenCard') { amt = round2(currentPaid.value * r.staffReferralPercent / 100); desc = `开卡实收 ${r.staffReferralPercent}%`; }
  return amt > 0
    ? `员工推荐人：提成 ¥${amt.toFixed(2)}（${desc}）→ 计入该员工当月工资`
    : '员工推荐人：暂无提成（推荐规则未开启或额度为 0）';
});

const hasReferralPreview = computed(() => !!(customerRewardPreview.value || staffRewardPreview.value));

function onCreateTypePicked(id: number | null) {
  const t = memberTypes.value.find((x) => x.id === id);
  if (!t) return;
  // 折扣以类型为准
  form.discount = t.discount;
  if (t.kind === 'StoredValue') {
    form.initialBalance = t.minRechargeAmount ?? 0;
    form.count = 0;
  } else {
    form.count = t.minPurchaseCount ?? 1;
    form.initialBalance = 0; // 计次卡的"实收金额"由收银员根据现金价填，默认 0
  }
}

// ---- 办卡相关（针对已有会员追加一张卡）----
const issueOpen = ref(false);
const issueTarget = ref<Member | null>(null);
const issueForm = reactive({
  memberTypeId: null as number | null,
  amount: 0,
  count: 1,
  payMethod: 'Wechat',
  remark: ''
});
const selectedIssueType = computed<MemberType | null>(() =>
  memberTypes.value.find((t) => t.id === issueForm.memberTypeId) ?? null);

async function openIssueCard(row: Member) {
  issueTarget.value = row;
  issueForm.memberTypeId = null;
  issueForm.amount = 0;
  issueForm.count = 1;
  issueForm.payMethod = 'Wechat';
  issueForm.remark = '';
  issueOpen.value = true;
  await ensureMemberTypesLoaded();
}

function onIssueTypeChange(id: number | null) {
  const t = memberTypes.value.find((x) => x.id === id);
  if (!t) return;
  if (t.kind === 'StoredValue') {
    issueForm.amount = t.minRechargeAmount ?? 0;
    issueForm.count = 0;
  } else {
    issueForm.count = t.minPurchaseCount ?? 1;
    issueForm.amount = 0;
  }
}

async function doIssueCard() {
  if (!issueTarget.value || !selectedIssueType.value) return;
  const t = selectedIssueType.value;
  if (t.kind === 'StoredValue') {
    if (issueForm.amount < (t.minRechargeAmount ?? 0)) {
      ElMessage.warning(`充值金额不能低于 ¥${(t.minRechargeAmount ?? 0).toFixed(2)}`);
      return;
    }
  } else {
    if (issueForm.count < (t.minPurchaseCount ?? 1)) {
      ElMessage.warning(`购买次数不能低于 ${t.minPurchaseCount ?? 1}`);
      return;
    }
  }
  saving.value = true;
  try {
    const r = await membersApi.issueCard(issueTarget.value.id, {
      memberTypeId: t.id,
      amount: issueForm.amount,
      count: issueForm.count,
      payMethod: issueForm.payMethod,
      remark: issueForm.remark || null
    });
    if (t.kind === 'StoredValue') {
      ElMessage.success(`办卡成功，余额到账 ¥${r.newBalance.toFixed(2)}（含赠送 ¥${r.bonusAmount.toFixed(2)}）`);
    } else {
      ElMessage.success(`办卡成功，已发放 ${issueForm.count + r.bonusCount} 次「${t.serviceItemName ?? ''}」（含赠送 ${r.bonusCount} 次）`);
    }
    issueOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

/// el-select remote-method：按手机号聚合搜索，相同会员只出一行（与收银台开台引荐人风格一致）
async function searchReferrer(keyword: string) {
  const k = keyword.trim();
  if (!k) { referrerSearchResults.value = []; return; }
  referrerSearchLoading.value = true;
  try {
    const r = await membersApi.grouped({
      keyword: k, pageSize: 10, includeClosed: false,
      storeId: appStore.activeStoreId ?? undefined
    });
    referrerSearchResults.value = r.items;
  } catch {
    referrerSearchResults.value = [];
  } finally {
    referrerSearchLoading.value = false;
  }
}

function referrerOptionLabel(g: MemberPhoneGroup): string {
  const name = g.primaryName ?? '未填';
  return `${g.phone} · ${name} · ${g.cardCount} 张卡`;
}

/// 选中"人"后挑该人名下首张可用卡满足后端 referredByMemberId（cardId）参数
function onReferrerSelected(phone: string | null) {
  if (!phone) {
    form.referrerPhone = '';
    form.referrerLabel = '';
    form.referredByMemberId = null;
    return;
  }
  const group = referrerSearchResults.value.find((g) => g.phone === phone);
  if (!group) return;
  const activeCard = group.cards.find((c) => c.isActive) ?? group.cards[0];
  form.referrerPhone = phone;
  form.referrerLabel = referrerOptionLabel(group);
  form.referredByMemberId = activeCard?.id ?? null;
}

/// 清除已选顾客引荐人（含编辑时带出的原引荐人）：编辑保存时配合 updateReferredByMember=true 落库为 null
function clearReferrer() {
  form.referrerPhone = '';
  form.referrerLabel = '';
  form.referredByMemberId = null;
}
const rules: FormRules = {
  cardNo: [{ required: true, message: '请输入卡号', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  discount: [{ required: true, message: '请输入折扣', trigger: 'blur' }],
  memberTypeId: [{ required: true, message: '请选择会员类型', trigger: 'change' }]
};

const rechargeOpen = ref(false);
const rechargeTarget = ref<Member | null>(null);
const rcForm = reactive({ amount: 100, bonusAmount: 0, count: 1, payMethod: 'Wechat', remark: '' });

/// 当前充值目标会员所属的会员类型；卡上没有 memberTypeId 则为 null（走旧逻辑）
const rechargeType = computed<MemberType | null>(() => {
  const t = rechargeTarget.value;
  if (!t?.memberTypeId) return null;
  return memberTypes.value.find((x) => x.id === t.memberTypeId) ?? null;
});

/// 充值次卡时的实充次数 = 充值次数 + 赠送次数
const rechargeCreditCount = computed(() => {
  const bonus = rechargeType.value?.bonusCount ?? 0;
  return rcForm.count + bonus;
});

/// 充值次卡：绑定的服务（含会员价），用来推算金额
const rechargeBoundService = computed<ServiceItem | null>(() => {
  const t = rechargeType.value;
  if (!t || t.kind !== 'CountBased' || !t.serviceItemId) return null;
  return services.value.find((s) => s.id === t.serviceItemId) ?? null;
});

/// 充值次卡：服务单价（优先会员价，否则标准价）
const rechargeBoundUnitPrice = computed(() => {
  const s = rechargeBoundService.value;
  if (!s) return 0;
  return s.memberPrice > 0 ? s.memberPrice : s.price;
});

/// 充值次卡：充值金额（面值）= 充值次数 × 会员价
const rechargeCountFaceAmount = computed(() =>
  Math.round(rcForm.count * rechargeBoundUnitPrice.value * 100) / 100
);

/// 充值次卡：实收金额 = 充值金额 × 折扣（折扣以类型为准）
const rechargeCountChargeAmount = computed(() => {
  const d = rechargeType.value?.discount ?? 1;
  return Math.round(rechargeCountFaceAmount.value * d * 100) / 100;
});

/// 充值卡：实收金额 = 充值金额 × 折扣
const rechargeChargeAmount = computed(() => {
  const d = rechargeType.value?.discount ?? 1;
  return Math.round(rcForm.amount * d * 100) / 100;
});

/// 充值卡：实充金额 = 充值金额 + 赠送（卡内余额）
const rechargeCreditAmount = computed(() => {
  const bonus = rechargeType.value?.bonusAmount ?? 0;
  return Math.round((rcForm.amount + bonus) * 100) / 100;
});

const historyOpen = ref(false);
const historyTarget = ref<Member | null>(null);
const historyTab = ref<'recharge' | 'consume'>('recharge');
const rechargeList = ref<any[]>([]);
const orderList = ref<any[]>([]);

async function reload() {
  loading.value = true;
  try {
    const data = await membersApi.grouped({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      storeId: appStore.activeStoreId ?? undefined,
      includeClosed: query.includeClosed
    });
    groups.value = data.items;
    total.value = data.total;
    // 数据换了一批，之前展开的可能已不在结果集，整体收起避免空 id 残留
    expandedPhones.value = new Set();
  } finally {
    loading.value = false;
  }
}

const RECHARGE_KIND_LABEL: Record<string, string> = {
  Recharge: '充值', Refund: '退卡', TransferOut: '转出', TransferIn: '转入', ReferralBonus: '返佣'
};
const RECHARGE_KIND_TAG: Record<string, 'info' | 'warning' | 'success' | 'danger'> = {
  Recharge: 'success', Refund: 'danger', TransferOut: 'warning', TransferIn: 'info', ReferralBonus: 'success'
};
function rechargeKindLabel(k: string) { return RECHARGE_KIND_LABEL[k] ?? k; }
function rechargeKindTag(k: string) { return RECHARGE_KIND_TAG[k] ?? 'info'; }

/// 卡号默认折叠，按"会员（手机号）"维度展开/收起；切换数据源后整体收起。
/// 用 Set 存当前展开的 phone；mutation 后整体替换以触发 Vue 响应式
const expandedPhones = ref<Set<string>>(new Set());
function isExpanded(phone: string) {
  return expandedPhones.value.has(phone);
}
function toggleExpand(phone: string) {
  const next = new Set(expandedPhones.value);
  if (next.has(phone)) next.delete(phone);
  else next.add(phone);
  expandedPhones.value = next;
}

/// 把数字金额读成"123 元 4 角"，给读屏软件听，避免它把"123.40"读成"一百二十三点四零"
function yuanReadable(amount: number): string {
  const safe = Number.isFinite(amount) ? amount : 0;
  const yuan = Math.floor(safe);
  const jiao = Math.round((safe - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}

/// 一行总述给读屏念出来，避免它逐字段碎读
function groupAriaLabel(g: MemberPhoneGroup): string {
  const name = g.primaryName || '未填姓名';
  const closed = g.hasInactive ? '，含已关闭的卡' : '';
  return `${name}，手机 ${g.phone}，持卡 ${g.cardCount} 张${closed}，总余额 ${yuanReadable(g.totalBalance)}`;
}

function cardAriaLabel(c: Member, primaryName: string | null | undefined): string {
  const name = primaryName || '未填姓名';
  const type = c.memberTypeName ?? '普通卡';
  const status = c.isActive ? '使用中' : '已关闭';
  const discount = c.discount < 1 ? `，折扣 ${(c.discount * 10).toFixed(1)} 折` : '';
  const countSuffix = c.memberTypeKind === 'CountBased'
    ? `，累计 ${c.totalCount ?? 0} 次，剩 ${c.remainCount ?? 0} 次`
    : '';
  return `${name} 的卡 ${c.cardNo}，${type}，${status}${discount}，余额 ${yuanReadable(c.balance)}${countSuffix}`;
}

/// 开卡时间（= 会员创建时间）
function cardStartText(c: Member): string {
  return dayjs(c.createdAt).format('YYYY-MM-DD HH:mm:ss');
}

/// 有效期文案：无到期=永久；到期只显示日期（到当天 23:59:59）
function cardValidityText(c: Member): string {
  if (!c.cardExpiresAt) return '永久有效';
  const exp = dayjs(c.cardExpiresAt).format('YYYY-MM-DD');
  const d = c.cardDaysRemaining;
  if (d == null) return `到期 ${exp}`;
  if (d < 0) return `已过期（${exp}）`;
  if (d === 0) return `今天到期（${exp}）`;
  return `到期 ${exp}（剩 ${d} 天）`;
}

/// 给读屏用的有效期朗读串（到期只读日期）
function cardValidityAria(c: Member): string {
  if (!c.cardExpiresAt) return '永久有效';
  const exp = dayjs(c.cardExpiresAt).format('YYYY-MM-DD');
  const d = c.cardDaysRemaining;
  if (d == null) return `到期日 ${exp}`;
  if (d < 0) return `已过期，到期日 ${exp}`;
  if (d === 0) return `今天到期，${exp}`;
  return `到期日 ${exp}，还有 ${d} 天过期`;
}

function resetQuery() {
  query.keyword = '';
  query.page = 1;
  reload();
}

const phoneLocked = ref(false);

async function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  phoneLocked.value = false;
  Object.assign(form, {
    cardNo: '', phone: '', name: '', gender: '', birthday: '',
    discount: 1, initialBalance: 0, remark: '',
    referrerPhone: '', referrerLabel: '', referredByMemberId: null, referredByStaffId: null,
    memberTypeId: null, count: 0, payMethod: 'Wechat'
  });
  referrerSearchResults.value = [];
  formOpen.value = true;
  // 异步加载会员类型 + 推荐上下文（员工列表/推荐规则），不阻塞打开对话框
  await Promise.all([ensureMemberTypesLoaded(), ensureReferralContextLoaded()]);
}

/// 在已有手机号下加办一张卡：预填手机号并锁定，姓名也带过来
async function openCreateForPhone(phone: string) {
  await openCreate();
  const group = groups.value.find((g) => g.phone === phone);
  form.phone = phone;
  if (group?.primaryName) form.name = group.primaryName;
  phoneLocked.value = true;
}

async function openEdit(row: Member) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    cardNo: row.cardNo,
    phone: row.phone,
    name: row.name ?? '',
    gender: row.gender ?? '',
    birthday: row.birthday ?? '',
    discount: row.discount,
    initialBalance: 0,
    remark: row.remark ?? '',
    // 已有引荐人时在下拉里带出当前项（哨兵 value + 姓名 label）；可重新搜索改人，或清除
    referrerPhone: row.referredByMemberId ? editReferrerSentinel : '',
    referrerLabel: row.referredByMemberName ?? '',
    referredByMemberId: row.referredByMemberId ?? null,
    referredByStaffId: row.referredByStaffId ?? null,
    memberTypeId: null,
    count: 0,
    payMethod: 'Wechat'
  });
  referrerSearchResults.value = [];
  formOpen.value = true;
  // 加载本店员工列表，供员工推荐人下拉预选/改派
  await ensureReferralContextLoaded();
}

async function saveForm() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  if (!appStore.activeStoreId) {
    ElMessage.error('未选择门店');
    return;
  }
  saving.value = true;
  try {
    if (formMode.value === 'create') {
      // 开卡必选会员类型
      if (!form.memberTypeId) {
        ElMessage.warning('请选择会员类型');
        return;
      }
      const t = selectedCreateType.value;
      if (t) {
        if (t.kind === 'StoredValue' && form.initialBalance < (t.minRechargeAmount ?? 0)) {
          ElMessage.warning(`充值金额不能低于 ¥${(t.minRechargeAmount ?? 0).toFixed(2)}`);
          return;
        }
        if (t.kind === 'CountBased' && form.count < (t.minPurchaseCount ?? 1)) {
          ElMessage.warning(`购买次数不能低于 ${t.minPurchaseCount ?? 1}`);
          return;
        }
      }
      // 计次卡的 initialBalance 走"实收金额"（次数 × 会员价 × 折扣）记账
      const paidAmount = t?.kind === 'CountBased' ? countChargeAmount.value : form.initialBalance;
      await membersApi.create({
        storeId: appStore.activeStoreId,
        cardNo: form.cardNo,
        phone: form.phone,
        name: form.name || null,
        gender: form.gender || null,
        birthday: form.birthday || null,
        discount: form.discount,
        initialBalance: paidAmount,
        remark: form.remark || null,
        referredByMemberId: form.referredByMemberId,
        referredByStaffId: form.referredByStaffId,
        memberTypeId: form.memberTypeId,
        count: form.count,
        payMethod: form.payMethod
      });
    } else if (editingId.value != null) {
      await membersApi.update(editingId.value, {
        phone: form.phone,
        name: form.name || null,
        gender: form.gender || null,
        birthday: form.birthday || null,
        discount: form.discount,
        remark: form.remark || null,
        referredByMemberId: form.referredByMemberId,
        updateReferredByMember: true,
        referredByStaffId: form.referredByStaffId,
        updateStaffReferral: true
      });
    }
    ElMessage.success('已保存');
    formOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function openRecharge(row: Member) {
  if (!row.isActive) {
    ElMessage.warning('该卡已退卡 / 关闭，不能再充值');
    return;
  }
  rechargeTarget.value = row;
  Object.assign(rcForm, { amount: 0, bonusAmount: 0, count: 1, payMethod: 'Wechat', remark: '' });
  rechargeOpen.value = true;
  await ensureMemberTypesLoaded();
  // 拿到模板后回填默认值
  const t = rechargeType.value;
  if (t?.kind === 'StoredValue') {
    rcForm.amount = t.minRechargeAmount ?? 100;
    rcForm.bonusAmount = t.bonusAmount ?? 0;
  } else if (t?.kind === 'CountBased') {
    rcForm.count = t.minPurchaseCount ?? 1;
    rcForm.amount = 0; // 实收金额由收银员输入
  } else {
    rcForm.amount = 100;
  }
}

async function doRecharge() {
  if (!rechargeTarget.value) return;
  const t = rechargeType.value;

  // 有模板：走 issueCard（按会员类型规则校验 + 自动赠送）
  if (t) {
    if (t.kind === 'StoredValue') {
      if (rcForm.amount < (t.minRechargeAmount ?? 0)) {
        ElMessage.warning(`充值金额不能低于 ¥${(t.minRechargeAmount ?? 0).toFixed(2)}`);
        return;
      }
    } else {
      if (rcForm.count < (t.minPurchaseCount ?? 1)) {
        ElMessage.warning(`充值次数不能低于 ${t.minPurchaseCount ?? 1}`);
        return;
      }
    }
    saving.value = true;
    try {
      const cashAmount = t.kind === 'CountBased' ? rechargeCountChargeAmount.value : rcForm.amount;
      const r = await membersApi.issueCard(rechargeTarget.value.id, {
        memberTypeId: t.id,
        amount: cashAmount,
        count: t.kind === 'CountBased' ? rcForm.count : 0,
        payMethod: rcForm.payMethod,
        remark: rcForm.remark || null
      });
      if (t.kind === 'StoredValue') {
        ElMessage.success(`充值成功，余额到账 ¥${r.newBalance.toFixed(2)}（含赠送 ¥${r.bonusAmount.toFixed(2)}）`);
      } else {
        ElMessage.success(`充值成功，已发放 ${rcForm.count + r.bonusCount} 次「${t.serviceItemName ?? ''}」（含赠送 ${r.bonusCount} 次）`);
      }
      rechargeOpen.value = false;
      reload();
    } finally {
      saving.value = false;
    }
    return;
  }

  // 无模板：走旧的 recharge
  if (rcForm.amount <= 0) {
    ElMessage.warning('充值金额必须 > 0');
    return;
  }
  saving.value = true;
  try {
    await membersApi.recharge({
      memberId: rechargeTarget.value.id,
      amount: rcForm.amount,
      bonusAmount: rcForm.bonusAmount,
      payMethod: rcForm.payMethod,
      remark: rcForm.remark || null
    });
    ElMessage.success(`充值成功，到账 ¥${(rcForm.amount + rcForm.bonusAmount).toFixed(2)}`);
    rechargeOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

async function openHistory(row: Member) {
  historyTarget.value = row;
  historyOpen.value = true;
  const [rs, os] = await Promise.all([
    membersApi.rechargeHistory(row.id),
    membersApi.consumptionHistory(row.id)
  ]);
  rechargeList.value = rs as any[];
  orderList.value = os as any[];
}

// ---- 退卡 ----
const refundOpen = ref(false);
const refundTarget = ref<Member | null>(null);
const rfForm = reactive({ refundAmount: 0, refundMethod: 'Wechat', reason: '' });

function openRefund(row: Member) {
  refundTarget.value = row;
  rfForm.refundAmount = row.balance;
  rfForm.refundMethod = 'Wechat';
  rfForm.reason = '';
  refundOpen.value = true;
}

async function doRefund() {
  if (!refundTarget.value) return;
  if (rfForm.refundAmount <= 0) { ElMessage.warning('退款金额必须 > 0'); return; }
  try {
    await ElMessageBox.confirm(
      `确认从「${refundTarget.value.name || refundTarget.value.cardNo}」退还 ¥${rfForm.refundAmount.toFixed(2)}，并关闭该卡？`,
      '退卡确认',
      { type: 'warning' }
    );
  } catch { return; }
  saving.value = true;
  try {
    await membersApi.refund(refundTarget.value.id, {
      refundAmount: rfForm.refundAmount,
      refundMethod: rfForm.refundMethod,
      reason: rfForm.reason || null
    });
    ElMessage.success('已退卡');
    refundOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

// ---- 转赠 ----
const transferOpen = ref(false);
const transferTarget = ref<Member | null>(null);
const tfForm = reactive({
  mode: 'existing' as 'existing' | 'new',
  toQuery: '',
  toMemberId: null as number | null,
  newMemberCardNo: '',
  newMemberPhone: '',
  newMemberName: '',
  reason: ''
});
const targetCandidates = ref<Member[]>([]);

function openTransfer(row: Member) {
  transferTarget.value = row;
  tfForm.mode = 'existing';
  tfForm.toQuery = '';
  tfForm.toMemberId = null;
  tfForm.newMemberCardNo = '';
  tfForm.newMemberPhone = '';
  tfForm.newMemberName = '';
  tfForm.reason = '';
  targetCandidates.value = [];
  transferOpen.value = true;
}

async function searchTarget() {
  if (!tfForm.toQuery.trim()) return;
  const r = await membersApi.list({
    keyword: tfForm.toQuery.trim(),
    page: 1, pageSize: 10,
    storeId: appStore.activeStoreId ?? undefined
  });
  targetCandidates.value = r.items.filter((m) => m.id !== transferTarget.value?.id && m.isActive);
  if (!targetCandidates.value.length) ElMessage.info('没有匹配的可用会员');
}

async function doTransfer() {
  if (!transferTarget.value) return;
  if (tfForm.mode === 'existing' && !tfForm.toMemberId) {
    ElMessage.warning('请选择目标会员');
    return;
  }
  if (tfForm.mode === 'new' && (!tfForm.newMemberCardNo || !tfForm.newMemberPhone)) {
    ElMessage.warning('新会员卡号和手机号必填');
    return;
  }
  saving.value = true;
  try {
    await membersApi.transfer(transferTarget.value.id, {
      toMemberId: tfForm.mode === 'existing' ? tfForm.toMemberId : null,
      newMemberCardNo: tfForm.mode === 'new' ? tfForm.newMemberCardNo : null,
      newMemberPhone: tfForm.mode === 'new' ? tfForm.newMemberPhone : null,
      newMemberName: tfForm.mode === 'new' ? tfForm.newMemberName || null : null,
      reason: tfForm.reason || null
    });
    ElMessage.success('已转赠');
    transferOpen.value = false;
    reload();
  } finally {
    saving.value = false;
  }
}

// ---- 引荐 ----
const referralsOpen = ref(false);
const referralsData = ref<ReferralSummaryDto | null>(null);

async function openReferrals(row: Member) {
  referralsData.value = await membersApi.referrals(row.id);
  referralsOpen.value = true;
}

onMounted(async () => {
  await appStore.loadStores();
  reload();
});
</script>

<style scoped>
/* 视口锁定：.page 撑满 .main、el-card 撑满 .page、卡片体内部 toolbar 固定、member-list 自滚 */
.page { height: 100%; display: flex; flex-direction: column; min-height: 0; }
.page > :deep(.el-card) { flex: 1 1 auto; display: flex; flex-direction: column; min-height: 0; }
.page > :deep(.el-card) > :deep(.el-card__body) {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; flex: 0 0 auto; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.metric-row { display: flex; gap: 32px; align-items: center; font-size: 14px; }
.opt-meta {
  float: right;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  margin-left: 24px;
}
:deep(.el-alert__content) { padding-right: 0; }
.type-summary { width: 100%; }
.permanent { color: var(--el-color-success); }
.member-form .el-form-item { margin-bottom: 12px; }
.member-form .full-row { width: 100%; }
.lock-tip { font-size: 12px; margin: -8px 0 8px 100px; }
.member-form :deep(.el-input-number) { width: 100%; }

/* —— 会员列表：卡片布局 ——
   显式 font-size 14（el-table 同档），去掉所有 font-weight 强调，让视觉密度对齐表格行。
   只保留结构性属性（间距、边框、布局方向）。
   无障碍模式由 App.vue 全局规则统一放大关键文字与留白。 */
.member-list {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 14px;
  flex: 1 1 auto;
  overflow: auto;
  min-height: 0;
}
.empty-tip { text-align: center; color: var(--el-text-color-secondary); padding: 48px 0; }

.member-card {
  border: 1px solid var(--el-border-color-light);
  border-radius: 6px;
  background: #fff;
  padding: 10px 14px;
}
.member-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding-bottom: 6px;
  border-bottom: 1px dashed var(--el-border-color-lighter);
}
.head-main { flex: 1 1 480px; min-width: 0; display: flex; flex-direction: column; gap: 4px; }
.head-line1 { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
/* 手机号 = 主键列，加粗强调（与其它列表里订单号 / 券码同款）；姓名走正常字重做对比 */
.phone { font-weight: 600; }
.head-stats { display: flex; gap: 16px; flex-wrap: wrap; }
.head-actions { display: flex; gap: 6px; }

.stat { display: inline-flex; align-items: baseline; gap: 4px; }
.stat-label { color: var(--el-text-color-secondary); }
.stat-val { /* 与表格 cell 同字重 */ }

.card-list { list-style: none; margin: 8px 0 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.card-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding: 8px 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 4px;
  background: #fafbfc;
}
.card-row.closed { background: #f5f5f5; opacity: 0.85; }
.card-row.closed .card-no { text-decoration: line-through; color: var(--el-text-color-secondary); }
.card-info { flex: 1 1 480px; min-width: 0; display: flex; flex-direction: column; gap: 4px; }
.card-line1 { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
/* 卡号 = 子卡的主键，加粗与手机号对齐 */
.card-no { font-weight: 600; }
.card-stats { display: flex; gap: 14px; flex-wrap: wrap; }
.card-validity { font-size: 12px; color: var(--el-text-color-regular); display: flex; align-items: center; flex-wrap: wrap; gap: 4px; }
.validity-val { color: #1f2937; }
.validity-val.expired { color: #d9534f; font-weight: 600; }
.card-actions { display: flex; gap: 6px; flex-wrap: wrap; }

/* 文本+颜色双重传达状态（盲人无障碍约定：不靠纯颜色） */
.badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 500;
  border: 1px solid transparent;
}
.badge-muted { background: #f0f0f0; color: #666; border-color: #ddd; }
.badge-info { background: #ecf5ff; color: #409eff; border-color: #b3d8ff; }
.badge-warning { background: #fdf6ec; color: #e6a23c; border-color: #f5dab1; }
.badge-success { background: #f0f9eb; color: #67c23a; border-color: #c2e7b0; }

/* 键盘焦点态强化，方便弱视用户定位 */
.member-card:focus-within { outline: 2px solid var(--el-color-primary); outline-offset: 2px; }
.card-row:focus-within { outline: 2px solid var(--el-color-primary); outline-offset: 1px; }

/* 开卡推荐奖励预估块 */
.referral-preview { width: 100%; background: #fff7e6; border: 1px solid #ffd591; border-radius: 6px; padding: 10px 12px; }
.rp-title { font-size: 12px; color: #b26a00; margin-bottom: 4px; }
.rp-line { font-size: 14px; font-weight: 600; color: #874d00; line-height: 1.7; }
</style>
