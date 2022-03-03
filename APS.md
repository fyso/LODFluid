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
  - $hi=\eta(\dfrac{m_i}{\rho_i})^{\frac13}, \quad\eta=1.1-1.2$
  - 对于可变的support radii, 密度的改变率为: $\dfrac{d\rho_i}{dt}=\dfrac{1}{\Omega_i}\sum\limits_jm_j\vec{v}_{ij}\nabla_iW_{ij}$
  - The gradient of a quantity: $\nabla A_i=\sum\limits_j(A_j-A_i)\dfrac{m_j}{\rho_j}\nabla_iW_{ij}$

- 对所有的混合粒子混合密度
  - $\rho_i^{blended}=(1-\beta_i)\rho_i+\beta_i\rho_O$
  - This blended density is used for all calculations the new particles are involved in.


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

- 更新所有粒子位置

### 4. 适应性分辨率

- 检测表面
  - 水平集方法
  - 有45个邻居的粒子被标记为内部粒子
  - smooth distance values: $\phi_i=\sum_j\frac{m_j}{\rho_j}\phi_jW_{ij}$

- 应用`sizing function`对粒子分类

  - adaptive factor: $\alpha=\dfrac{m^{fine}}{m^{base}}$

  - $m_i^{opt}(\phi_i)=m^{base}(\dfrac{min(|\phi_i|,|\phi_{max}|)}{|\phi_{max}|}(1-\alpha)+\alpha)$

  - Particle classification $m_i^{rel} = m_i / m_i^{opt}$

    $C_i=\begin{cases}
    S & m_i^{rel}<0.5\\
    s & 0.5 \le m_i^{rel} \le 0.9 \\
    o & 0.9 < m_i^{rel} < 1.1 \\
    l & 1.1 \le m_i^{rel} \le 2 \\
    L & 2 < m_i^{rel}
	  \end{cases}$
	
	- （在不能重新分配质量的情况下，$m_i$可能与$m_i^{opt}$偏离）

- 通过splitting创建粒子
  - 把L类的粒子拆分为n个，$n=\lceil{m_i}/{m_i^{opt}}\rceil$, $m_j=m_i/n$
  - 复制原本粒子的所有属性
  - uniform particles are placed evenly distanced on the surface of a sphere
  - with $n>4$, one of the particles is placed in the center
  - 检测和其他粒子距离，< 0.1需要旋转
  
- 寻找合并和再分配的同类

  - 搜索半径$\frac{h_i}{2}$
  - check $m_j+\frac{m_i}{n}<m^{base}$


- 合并粒子和再分配粒子

    - - 合并过程
    - `S 粒子`把质量分配给周围的`S or s`，使用`(n+1):n-pattern`
    - $A_j^*=\dfrac{A_im_n+A_jm_j}{m_j^*}$
    - - 再分配过程
    - for `l`: 重分配超过的质量给临近的`s`: $m_{ex} = m_i - m_i^{opt}$
    - $m_i^*=m_i-m_{ex},\quad m_j^*=m_j+\dfrac{m_{ex}}{n}$
    - $A_j^*=\dfrac{\frac{m_{ex}}{n}A_i+m_jA_j}{\frac{m_{ex}}{n}+m_j}$  (positions, velocities and surface distances)

