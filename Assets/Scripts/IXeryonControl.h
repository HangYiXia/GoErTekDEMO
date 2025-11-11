#pragma once
#ifdef XERYONCONTROL_EXPORTS
#define DLL_API __declspec(dllexport)
#else
#define DLL_API __declspec(dllimport)
#endif

// 轴的方向
enum class _Direction
{
	DIR_X = 0,
	DIR_Y,
	DIR_Z,
};

// 平台类型
enum class _Stage
{
	XLS_312 = 0,
	XLS_1250,
	XLS_78,
	XLS_5,
	XLS_1,
	XLS_312_3N,
	XLS_1250_3N,
	XLS_78_3N,
	XLS_5_3N,
	XLS_1_3N,
	XLA_312,
	XLA_1250,
	XLA_78,
	XRTA,
	XRTU_30_109,
	XRTU_40_73,
	XRTU_40_3,
};

class  IXeryonControl
{
public:
	virtual ~IXeryonControl() = default;
	// 开始通信，读取配置文件, 需要调用IAddAxis再调用，否则调用无效
	virtual void IStart() = 0;
	// 重置位置，重新读取配置文件
	virtual void IReset() = 0;
	// 重置位置，关闭通信
	virtual void IStop() = 0;
	// 添加一个方向的轴
	virtual void IAddAxis(_Direction dir, _Stage stage = _Stage::XLA_1250) = 0;
	// 设置位置，单位为um, Direction:坐标轴 X Y Z
	virtual void ISetDPOS(_Direction dir,double val_um) = 0;
	// 获取位置，单位为um, Direction:坐标轴 X Y Z
	virtual double IGetDPOS(_Direction dir) = 0;
	// 移动一个step的位置，单位为um, Direction:坐标轴 X Y Z
	virtual void IStep(_Direction dir, double val_um) = 0;
};

// 创建控制器， 需要指定串口和波特率
extern "C" DLL_API IXeryonControl* __stdcall CreateInstance(int port, int baudrate);
extern "C" DLL_API void __stdcall DestoryInstance(IXeryonControl* ctrlPtr);
// 开始通信，读取配置文件, 需要调用IAddAxis再调用，否则调用无效
extern "C" DLL_API void __stdcall IStart(IXeryonControl* ctrlPtr);
// 重置位置，重新读取配置文件
extern "C" DLL_API void __stdcall IReset(IXeryonControl* ctrlPtr);
// 重置位置，关闭通信
extern "C" DLL_API void __stdcall IStop(IXeryonControl* ctrlPtr);
// 添加一个方向的轴,需要指定设备
extern "C" DLL_API void __stdcall IAddAxis(IXeryonControl* ctrlPtr, _Direction dir, _Stage stage = _Stage::XLA_1250);
// 设置位置，单位为um, Direction:坐标轴 X Y Z
extern "C" DLL_API void __stdcall ISetDPOS(IXeryonControl* ctrlPtr, _Direction dir, double val_um);
// 获取位置，单位为um, Direction:坐标轴 X Y Z
extern "C" DLL_API double __stdcall IGetDPOS(IXeryonControl* ctrlPtr, _Direction dir);
// 移动一个step的位置，单位为um, Direction:坐标轴 X Y Z
extern "C" DLL_API void __stdcall IStep(IXeryonControl* ctrlPtr, _Direction dir, double val_um);