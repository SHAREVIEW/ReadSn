#include "reg51.h"

typedef unsigned char BYTE;
typedef unsigned int u8;

sbit Port0_0=P0^0;
sbit Port0_1=P0^1;
sbit Port0_2=P0^2;
sbit Port0_3=P0^3;
sbit Port0_4=P0^4;
sbit Port0_5=P0^5;
sbit Port0_6=P0^6;
sbit Port0_7=P0^7;

sbit Port1_0=P1^0;
sbit Port1_1=P1^1;
sbit Port1_2=P1^2;
sbit Port1_3=P1^3;
sbit Port1_4=P1^4;
sbit Port1_5=P1^5;
sbit Port1_6=P1^6;
sbit Port1_7=P1^7;

sbit Port2_0=P2^0;
sbit Port2_1=P2^1;
sbit Port2_2=P2^2;
sbit Port2_3=P2^3;
sbit Port2_4=P2^4;
sbit Port2_5=P2^5;
sbit Port2_6=P2^6;
sbit Port2_7=P2^7;

BYTE ReceiveBytes[3];

unsigned int receiveCount=0;


void initSerial(){
	TMOD=0x20;
	TH1=0xF3;
	TL1=0xF3;
	PCON=0x80;
	TR1=1;
	SCON=0x50;	//
	ES=1;	 //打开串口中断
	EA=1;  //打开总的终端
}

void delayms(u8 ms){	//delay毫秒数
	u8 i,j;
	for(i=ms;i>0;i--){
		for(j=110;j>0;j--);
	}	
}


void main(){
	P0=0xff;
	initSerial();
	while(1);
}


void Serial() interrupt 4{	//串口中断
	BYTE input; //输入的字符
	//u8 i;
	input=SBUF;
	while(!RI);
	RI=0;
	if(input==0x00){
		Port0_0=0; //设置P00口为低电平
		SBUF=Port0_0; //将P00口的电平发送到串口
		while(!TI);
		TI=0;
	}else if(input==0x01){
		Port0_0=1; //设置P00口为高电平
		SBUF=Port0_0; //将P00口的电平发送到串口
		while(!TI);
		TI=0;
	}else{
		SBUF=0x83;
		while(!TI);
		TI=0;
	}
}



