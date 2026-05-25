<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar" role="search">
        <el-input
          v-model="query.keyword"
          placeholder="卡号 / 手机号 / 姓名"
          clearable
          style="width: 240px"
          aria-label="搜索会员，输入卡号、手机号或姓名后回车"
          @keyup.enter="reload"
        />
        <el-button type="primary" aria-label="执行会员搜索" @click="reload">查询</el-button>
        <el-button aria-label="重置搜索条件" @click="resetQuery">重置</el-button>
        <el-button type="success" aria-label="开新会员卡" @click="openCreate">开卡</el-button>
      </div>

      <div class="toolbar" style="margin-top:8px">
        <el-checkbox v-model="query.includeClosed" @change="reload">显示已关闭会员</el-checkbox>
      </div>

      <el-table :data="groups" v-loading="loading" stripe style="margin-top: 12px" row-key="phone">
        <el-table-column type="expand">
          <template #default="{ row }">
            <el-table :data="row.cards" size="small" class="card-table" :show-header="true">
              <el-table-column label="卡号" width="120">
                <template #default="{ row: c }">
                  <span :class="{ closed: !c.isActive }">{{ c.cardNo }}</span>
                  <el-tag v-if="!c.isActive" size="small" type="info" style="margin-left:6px">已关闭</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="姓名" width="100" prop="name" />
              <el-table-column label="余额" width="110">
                <template #default="{ row: c }">
                  <strong style="color: #d9534f">¥{{ c.balance.toFixed(2) }}</strong>
                </template>
              </el-table-column>
              <el-table-column label="充值金额" width="110">
                <template #default="{ row: c }">¥{{ c.totalRecharge.toFixed(2) }}</template>
              </el-table-column>
              <el-table-column label="消费金额" width="110">
                <template #default="{ row: c }">¥{{ c.totalConsumed.toFixed(2) }}</template>
              </el-table-column>
              <el-table-column label="充值次数" width="140">
                <template #default="{ row: c }">
                  <template v-if="c.memberTypeKind === 'CountBased'">
                    <strong>{{ c.totalCount ?? 0 }}</strong> 次
                    <span class="muted" style="margin-left:4px">剩 {{ c.remainCount ?? 0 }} 次</span>
                  </template>
                  <span v-else class="muted">—</span>
                </template>
              </el-table-column>
              <el-table-column label="折扣" width="80">
                <template #default="{ row: c }">
                  <el-tag v-if="c.discount < 1" size="small" type="warning">{{ (c.discount * 10).toFixed(1) }}折</el-tag>
                  <span v-else class="muted">原价</span>
                </template>
              </el-table-column>
              <el-table-column label="会员卡类型" min-width="140">
                <template #default="{ row: c }">
                  <el-tag v-if="c.memberTypeName" size="small" :type="c.memberTypeKind === 'StoredValue' ? 'warning' : 'success'">
                    {{ c.memberTypeName }}
                  </el-tag>
                  <span v-else class="muted">—</span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="360" fixed="right">
                <template #default="{ row: c }">
                  <el-button link type="primary" :disabled="!c.isActive" @click="openRecharge(c)">充值</el-button>
                  <el-button link type="primary" @click="openHistory(c)">流水</el-button>
                  <el-button link type="primary" @click="openEdit(c)">编辑</el-button>
                  <el-button link type="warning" :disabled="!c.isActive || c.balance <= 0" @click="openRefund(c)">退卡</el-button>
                  <el-button link type="warning" :disabled="!c.isActive || c.balance <= 0" @click="openTransfer(c)">转赠</el-button>
                  <el-button link type="info" @click="openReferrals(c)">引荐</el-button>
                </template>
              </el-table-column>
            </el-table>
          </template>
        </el-table-column>

        <el-table-column label="手机号" width="160" prop="phone">
          <template #default="{ row }">
            <strong>{{ row.phone }}</strong>
            <el-tag v-if="row.hasInactive" size="small" type="info" style="margin-left:6px">含关闭</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="姓名" width="120">
          <template #default="{ row }">
            <span v-if="row.primaryName">{{ row.primaryName }}</span>
            <span v-else class="muted">未填</span>
          </template>
        </el-table-column>
        <el-table-column label="持卡数" width="90">
          <template #default="{ row }">{{ row.cardCount }} 张</template>
        </el-table-column>
        <el-table-column label="总余额" width="140">
          <template #default="{ row }">
            <strong style="color:#d9534f">¥{{ row.totalBalance.toFixed(2) }}</strong>
          </template>
        </el-table-column>
        <el-table-column label="累计充值" width="130">
          <template #default="{ row }">¥{{ row.totalRecharge.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="累计消费" width="130">
          <template #default="{ row }">¥{{ row.totalConsumed.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="操作" min-width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="success" @click="openCreateForPhone(row.phone)">加办一张卡</el-button>
          </template>
        </el-table-column>
      </el-table>

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

    <el-dialog v-model="formOpen" :title="formMode === 'create' ? '开卡' : '编辑会员'" width="520px">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="110px">
        <el-form-item label="卡号" prop="cardNo">
          <el-input v-model="form.cardNo" :disabled="formMode === 'edit'" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="form.phone" :disabled="formMode === 'create' && phoneLocked" />
          <span v-if="phoneLocked" class="muted" style="margin-left:8px">为该客户加办新卡，手机号已锁定</span>
        </el-form-item>
        <el-form-item label="姓名">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="性别">
          <el-radio-group v-model="form.gender">
            <el-radio value="男">男</el-radio>
            <el-radio value="女">女</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="生日">
          <el-date-picker v-model="form.birthday" type="date" placeholder="可选" value-format="YYYY-MM-DD" />
        </el-form-item>

        <el-form-item v-if="formMode === 'create'" label="会员类型" prop="memberTypeId" required>
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

        <el-form-item v-if="formMode === 'create' && selectedCreateType" label="类型规则">
          <el-descriptions :column="2" size="small" border class="type-summary">
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
            <el-descriptions-item v-if="selectedCreateType.kind === 'CountBased' && selectedCreateType.serviceItemName" label="绑定服务" :span="2">
              {{ selectedCreateType.serviceItemName }}
            </el-descriptions-item>
          </el-descriptions>
        </el-form-item>

        <el-form-item v-if="formMode === 'create' && selectedCreateType?.kind === 'StoredValue'" label="充值金额" prop="initialBalance" required>
          <el-input-number
            v-model="form.initialBalance"
            :min="selectedCreateType.minRechargeAmount ?? 0.01"
            :precision="2"
            :step="100"
          />
          <span class="muted" style="margin-left: 8px">
            卡面金额，最低 ¥{{ selectedCreateType.minRechargeAmount?.toFixed(2) }}
          </span>
        </el-form-item>

        <el-form-item v-if="formMode === 'create' && selectedCreateType?.kind === 'StoredValue'" label="实收金额">
          <strong style="color: #e6a23c; font-size: 16px">¥{{ chargeAmount.toFixed(2) }}</strong>
          <span class="muted" style="margin-left: 10px">
            充值金额 × {{ (form.discount * 10).toFixed(1) }} 折 = 客户实付
          </span>
        </el-form-item>

        <el-form-item v-if="formMode === 'create' && selectedCreateType?.kind === 'StoredValue'" label="实充金额">
          <strong style="color: #67c23a; font-size: 16px">¥{{ creditAmount.toFixed(2) }}</strong>
          <span class="muted" style="margin-left: 10px">
            充值金额 + 赠送 ¥{{ (selectedCreateType.bonusAmount ?? 0).toFixed(2) }} = 卡内余额
          </span>
        </el-form-item>

        <template v-if="formMode === 'create' && selectedCreateType?.kind === 'CountBased'">
          <el-form-item label="购买次数" prop="count" required>
            <el-input-number
              v-model="form.count"
              :min="selectedCreateType.minPurchaseCount ?? 1"
              :step="1"
            />
            <span class="muted" style="margin-left: 8px">
              最低 {{ selectedCreateType.minPurchaseCount }} 次
              <span v-if="(selectedCreateType.bonusCount ?? 0) > 0"> · 赠送 {{ selectedCreateType.bonusCount }} 次</span>
            </span>
          </el-form-item>

          <el-form-item label="实充次数">
            <strong style="color: #67c23a; font-size: 16px">{{ creditCount }} 次</strong>
            <span class="muted" style="margin-left: 10px">
              充值次数 + 赠送 {{ selectedCreateType.bonusCount ?? 0 }} 次 = 卡内可用次数
            </span>
          </el-form-item>

          <el-form-item label="充值金额">
            <strong style="font-size: 16px">¥{{ countFaceAmount.toFixed(2) }}</strong>
            <span class="muted" style="margin-left: 10px">
              <template v-if="boundService">
                {{ form.count }} 次 × 会员价 ¥{{ boundUnitPrice.toFixed(2) }}（{{ boundService.name }}）
              </template>
              <template v-else>
                绑定服务未配置会员价
              </template>
            </span>
          </el-form-item>

          <el-form-item label="实收金额">
            <strong style="color: #e6a23c; font-size: 16px">¥{{ countChargeAmount.toFixed(2) }}</strong>
            <span class="muted" style="margin-left: 10px">
              充值金额 × {{ (form.discount * 10).toFixed(1) }} 折 = 客户实付
            </span>
          </el-form-item>
        </template>

        <el-form-item v-if="formMode === 'create'" label="支付来源" prop="payMethod" required>
          <el-radio-group v-model="form.payMethod">
            <el-radio-button value="Wechat">微信</el-radio-button>
            <el-radio-button value="Alipay">支付宝</el-radio-button>
            <el-radio-button value="BankCard">银行卡</el-radio-button>
            <el-radio-button value="Cash">现金</el-radio-button>
            <el-radio-button value="Other">其它</el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="折扣" prop="discount">
          <el-input-number
            v-model="form.discount"
            :min="0.1"
            :max="1"
            :step="0.05"
            :precision="2"
            :disabled="formMode === 'create'"
          />
          <span class="muted" style="margin-left: 8px">
            <template v-if="formMode === 'create'">由会员类型决定，不可手改</template>
            <template v-else>如 0.85 = 8.5 折</template>
          </span>
        </el-form-item>

        <el-form-item label="引荐人">
          <el-input v-model="form.referrerKeyword" placeholder="卡号 / 手机号 搜索后选择" @keyup.enter="searchReferrer" />
          <el-button link size="small" @click="searchReferrer">查找</el-button>
          <el-radio-group v-if="referrerCandidates.length" v-model="form.referredByMemberId" style="margin-top:6px; display:flex; flex-direction:column; gap:6px">
            <el-radio v-for="c in referrerCandidates" :key="c.id" :value="c.id">
              {{ c.cardNo }}（{{ c.name || '未填' }} / {{ c.phone }}）
            </el-radio>
          </el-radio-group>
          <span v-if="form.referredByMemberId" class="muted">已选引荐人 ID = {{ form.referredByMemberId }} <el-button link type="danger" size="small" @click="form.referredByMemberId = null">清除</el-button></span>
        </el-form-item>
        <el-form-item label="备注">
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
            <strong style="color: #67c23a; font-size:16px">{{ rechargeCreditCount }} 次</strong>
            <span class="muted" style="margin-left:8px">充值次数 + 赠送 {{ rechargeType.bonusCount ?? 0 }} 次 = 卡内可用次数</span>
          </el-form-item>
          <el-form-item label="充值金额">
            <strong style="font-size:16px">¥{{ rechargeCountFaceAmount.toFixed(2) }}</strong>
            <span class="muted" style="margin-left:8px">
              <template v-if="rechargeBoundService">
                {{ rcForm.count }} 次 × 会员价 ¥{{ rechargeBoundUnitPrice.toFixed(2) }}（{{ rechargeBoundService.name }}）
              </template>
              <template v-else>绑定服务未配置会员价</template>
            </span>
          </el-form-item>
          <el-form-item label="实收金额">
            <strong style="color:#e6a23c; font-size:16px">¥{{ rechargeCountChargeAmount.toFixed(2) }}</strong>
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
              <strong style="color:#e6a23c; font-size:16px">¥{{ rechargeChargeAmount.toFixed(2) }}</strong>
              <span class="muted" style="margin-left:8px">
                充值金额 × {{ ((rechargeType.discount ?? 1) * 10).toFixed(1) }} 折 = 客户实付
              </span>
            </el-form-item>
            <el-form-item label="实充金额">
              <strong style="color:#67c23a; font-size:16px">¥{{ rechargeCreditAmount.toFixed(2) }}</strong>
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

    <el-drawer v-model="historyOpen" title="会员流水" size="640px">
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
            <el-table-column prop="payMethod" label="支付" width="80" />
            <el-table-column label="对手会员" width="120">
              <template #default="{ row }">{{ row.counterpartyMemberName || '—' }}</template>
            </el-table-column>
            <el-table-column label="时间" min-width="140">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="消费记录" name="consume">
          <el-table :data="orderList" size="small">
            <el-table-column prop="orderNo" label="订单" min-width="160" />
            <el-table-column prop="paidAmount" label="实收" width="100">
              <template #default="{ row }">¥{{ row.paidAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="80" />
            <el-table-column label="时间" min-width="140">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
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
            <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
          </el-table-column>
        </el-table>
        <p v-if="referralsData.referredCount === 0" class="muted" style="text-align:center; padding:30px 0">还没有引荐过会员</p>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import dayjs from 'dayjs';
import { memberTypesApi, membersApi, servicesApi, type MemberType, type ReferralSummaryDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Member, MemberPhoneGroup, ServiceItem } from '@/api/types';

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
  referrerKeyword: '',
  referredByMemberId: null as number | null,
  memberTypeId: null as number | null,
  count: 0,
  payMethod: 'Wechat'
});
const referrerCandidates = ref<Member[]>([]);

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

async function searchReferrer() {
  if (!form.referrerKeyword.trim()) return;
  const r = await membersApi.list({
    keyword: form.referrerKeyword.trim(),
    page: 1, pageSize: 10,
    storeId: appStore.activeStoreId ?? undefined
  });
  referrerCandidates.value = r.items.filter((m) => m.isActive);
  if (!referrerCandidates.value.length) ElMessage.info('没有匹配的可用会员');
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
    referrerKeyword: '', referredByMemberId: null,
    memberTypeId: null, count: 0, payMethod: 'Wechat'
  });
  referrerCandidates.value = [];
  formOpen.value = true;
  // 异步加载会员类型不阻塞打开对话框
  await ensureMemberTypesLoaded();
}

/// 在已有手机号下加办一张卡：预填手机号并锁定，姓名也带过来
async function openCreateForPhone(phone: string) {
  await openCreate();
  const group = groups.value.find((g) => g.phone === phone);
  form.phone = phone;
  if (group?.primaryName) form.name = group.primaryName;
  phoneLocked.value = true;
}

function openEdit(row: Member) {
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
    referrerKeyword: '',
    referredByMemberId: row.referredByMemberId ?? null,
    memberTypeId: null,
    count: 0,
    payMethod: 'Wechat'
  });
  referrerCandidates.value = [];
  formOpen.value = true;
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
        referredByMemberId: form.referredByMemberId
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
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.muted { color: var(--el-text-color-secondary); font-size: 12px; }
.closed { color: #999; text-decoration: line-through; }
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
</style>
