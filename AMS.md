## 基本步骤：
重排序和邻居列表

计算密度

粒子整合

适应分辨率



## 算法细节

### 1. 粒子排序和邻近列表

- Sort Particles

- Build neighborlists for all particles

###  2. 计算密度

- 计算所有粒子$\rho_i = \sum_jm_jW_{ij}$

- 对所有的混合粒子混合密度

###  3. 粒子整合

- 计算对流力(表面张力、粘滞力等) $F_i^{adv}$
- 对混合粒子混合速度 
   - $\vec{v}_O$为其他相关粒子的均值，$x_O$根据$\vec{v}_O$来更新，$\rho_O$根据$x_O$来计算
  - $\vec{v}^{blended}_i=(1-\beta_i)\vec{v}_i+\beta_i\vec{v}_O$
  - $\beta_i = \begin{cases}0.5, \ if\ splitting \\0.2,\ if \ merging\end{cases}, \Delta\beta_i = -0.1$
  - 粒子在混合系数为0之前不参与分裂、合并以及重新分配。
- 施行不可压缩性，利用改进的压力
   - $F_i^P=\sum\limits_j\dfrac{m_j}{\rho_j}(\dfrac{P_i}{\rho_i^2\Omega_i}+\dfrac{P_i}{\rho_j^2\Omega_j})\nabla_iW_{ij}$
   - $\Omega_i=\begin{cases}1+\dfrac{h_i}{3\rho_i}m_i\dfrac{\partial W_{ii}}{\partial h_{ii}}，& C^{t-1}_i=l\\
   1+\dfrac{h_i}{3\rho_i}\sum_jm_j\dfrac{\partial W_{ij}}{\partial h_{ij}}, & else\end{cases}$

