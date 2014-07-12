package com.example.screencaster1;

import android.app.Activity;
import android.view.WindowManager.LayoutParams;
import android.app.ActionBar;
import android.app.Fragment;
import android.content.Context;
import android.os.Bundle;
import android.text.format.Time;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.os.Build;
import android.os.Handler;
import android.widget.ImageView;
import android.widget.TextView;
import android.graphics.*;

import java.io.BufferedReader;
import java.io.DataInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

public class MainActivity extends Activity {

	
	private ServerSocket serverSocket;

	Thread serverThread = null;
	public ArrayList<ClientConnectionThread> clientConnections = new ArrayList<ClientConnectionThread>();
	
	public static Bitmap mainBitmap = null;
	
	public static View v = null;
	
	

	public static final int SERVERPORT = 6000;
	
	public static final int CASTER_HEADER_LENGTH = 24;
	
	
	
  
    @Override
	public void onCreate(Bundle savedInstanceState) {

		super.onCreate(savedInstanceState);
		requestWindowFeature(Window.FEATURE_NO_TITLE);
	    getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, 
	                            WindowManager.LayoutParams.FLAG_FULLSCREEN);
	    getWindow().addFlags(LayoutParams.FLAG_KEEP_SCREEN_ON);
		v = new myView(this);
		
		setContentView(v);

		this.serverThread = new Thread(new ServerThread());
		this.serverThread.start();

	}


    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();
        if (id == R.id.action_settings) {
            return true;
        }
        return super.onOptionsItemSelected(item);
    }
    
    @Override
	protected void onStop() {
		super.onStop();
		try {
			serverSocket.close();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}
    
    private class myView extends View{

    	 public myView(Context context) {
    	  super(context);  
    	  // TODO Auto-generated constructor stub
    	 }
    	 
    	 @Override
    	 protected void onMeasure(int widthMeasureSpec, int heightMeasureSpec) {
    	     super.onMeasure(widthMeasureSpec, heightMeasureSpec);

    	     int parentWidth = MeasureSpec.getSize(widthMeasureSpec);
    	     int parentHeight = MeasureSpec.getSize(heightMeasureSpec);
    	     this.setMeasuredDimension(
    	             parentWidth, parentHeight);
    	     Log.d("JHHMSG","setting size to "+parentWidth+"x"+parentHeight);
    	 }
    	 
    	 @Override
    	 protected void onDraw(Canvas canvas) {
    		 int height=this.getMeasuredHeight();
    		 int width=this.getMeasuredWidth();
    		 Log.d("JHHMSG","Drawing canvas");
    	  // TODO Auto-generated method stub
    		 
    		 ClientConnectionThread bestThread = null;
    		 for(ClientConnectionThread cct :clientConnections){
    			 if(bestThread==null) {
    				 bestThread = cct;
    			 } else {
    				 if(cct.connectTime.after(bestThread.connectTime)) bestThread = cct;
    			 }
    		 }
    		 
    		 if((bestThread!=null)&&(bestThread.clientBitmap!=null)) {
    			 Log.d("JHHMSG","creating scaled bitmap size "+width+"x"+height);
    			 //Bitmap newBitmap = Bitmap.createScaledBitmap(bestThread.clientBitmap, width, height, true);
    			 Bitmap newBitmap = bestThread.clientBitmap;
    			 if(newBitmap!=null) mainBitmap = newBitmap; else {
    				 Log.d("JHHMSG","failed to create scaled bitmap");
    			 }
    			  
    	          canvas.drawBitmap(mainBitmap, 0, 0, null);
    	          Log.d("JHHMSG","Drawing bitmap");
    	 
    	          int mouseX = bestThread.mouseX;
    	          int mouseY = bestThread.mouseY;
    	          Paint mp3 = new Paint();
    	          mp3.setARGB(200,255 , 255, 255);
    	          mp3.setStrokeWidth(2);
    	          canvas.drawCircle(mouseX, mouseY, 3, mp3);
    	          Paint mp = new Paint();
    	          mp.setARGB(255,0 , 0, 0);
    	          mp.setStrokeWidth(2);
    	          canvas.drawCircle(mouseX, mouseY, 1, mp);
    	          Paint mp2 = new Paint();
    	          mp2.setARGB(100,255 ,255, 255);
    	          mp2.setStrokeWidth(8);
    	          canvas.drawCircle(mouseX, mouseY, 10, mp2);
    	  
    		 }
    	 
    	 }
    }

    
    class ServerThread implements Runnable {

		public void run() {
			Socket socket = null;
			try {
				serverSocket = new ServerSocket(SERVERPORT);
			} catch (IOException e) {
				e.printStackTrace();
			}
			while (true) {

				try {

					socket = serverSocket.accept();

					ClientConnectionThread commThread = new ClientConnectionThread(socket);
					clientConnections.add(commThread);
					Log.d("JHHMSG","Got a connection, running commthread");
					new Thread(commThread).start();
					Log.d("JHHMSG","back from running commthread");

				} catch (IOException e) {
					e.printStackTrace();
					Log.d("JHHMSG","exception in main server loop");
				}
			}
		}
	}
    
    class ClientConnectionThread implements Runnable {

    	public Time connectTime = new Time();
		private Socket clientSocket;

		private DataInputStream dis;
		
		private byte[] inputBuf = new byte[(1920*1200*4)+4096];
		
		private byte[] uncompressedBuf = new byte [(1920*1200*4)+4096];
		
		private byte[] currentImageBuf = new byte [(1920*1200*4)+1024];
		
		public int mouseX = 0;
		public int mouseY = 0;
		public Bitmap clientBitmap = null;
		
		private int currentImageBufLength = 0;

		public ClientConnectionThread(Socket clientSocket) {

			this.clientSocket = clientSocket;
			this.connectTime.setToNow();
			

			try {

				this.dis = new DataInputStream(this.clientSocket.getInputStream());

			} catch (IOException e) {
				e.printStackTrace();
				Log.d("JHHMSG","exception in clientsocket init");
			}
		}
		
		public int readIntFromArrayMSB(byte[] array,int offset){
			return ((array[offset++] & 0xff) << 24) | ((array[offset++] & 0xff) << 16) | ((array[offset++] & 0xff) << 8) | (array[offset++] & 0xff);
		}
		public int readIntFromArrayLSB( byte[] array,int offset){
			
			return ((array[offset+3] & 0xff) << 24) | ((array[offset+2] & 0xff) << 16) | ((array[offset+1] & 0xff) << 8) | (array[offset] & 0xff);
		}
		
		private void copyNewBitmapBufferIntoMainBitmap(byte [] mainBitmap,byte [] newBitmap, int xOffset, int yOffset,int newBitmapLength)
		{
			int mainHeaderLength = readIntFromArrayLSB(mainBitmap,10);
			int newHeaderLength = readIntFromArrayLSB(newBitmap,10);
			//for(int i=0;i<50;i++){
			//	Log.d("JHHMSG","newBitmap "+i+" = "+(newBitmap[i] & 0xff));
			//}
			if(mainHeaderLength==0){
				// this is our first copy, so just duplicate the new bitmap into the main bitmap.
				for(int i=0;i<newBitmapLength;i++) {
					mainBitmap[i]=newBitmap[i];
				}
				return;
			}
			
			Log.d("JHHMSG","newBitmap bmp header total is "+newHeaderLength);
			int mainBitmapWidth = readIntFromArrayLSB(mainBitmap,18);
			int mainBitmapHeight = readIntFromArrayLSB(mainBitmap,22);
			
			int newBitmapWidth = readIntFromArrayLSB(newBitmap,18);
			int newBitmapHeight = readIntFromArrayLSB(newBitmap,22);
			Log.d("JHHMSG","newBitmap size "+newBitmapWidth+"x"+newBitmapHeight);
			Log.d("JHHMSG","mainBitmap size "+mainBitmapWidth+"x"+mainBitmapHeight);
			
			int newBitmapIndex = 0;
			int mainBitmapIndex = 0;
			yOffset = 1080-yOffset-newBitmapHeight;
			if(yOffset<0) yOffset=0;
			
			Log.d("JHHMSG","mainBitmap header length "+mainHeaderLength);
			for(int y=0;y<newBitmapHeight;y++){
				newBitmapIndex = (y*newBitmapWidth*4)+newHeaderLength;
				mainBitmapIndex = ((((y+yOffset)*mainBitmapWidth)+xOffset)*4)+mainHeaderLength;
				
				
				
				for(int x = 0;x<newBitmapWidth;x++){
					mainBitmap[mainBitmapIndex++] = newBitmap[newBitmapIndex++];
					mainBitmap[mainBitmapIndex++] = newBitmap[newBitmapIndex++];
					mainBitmap[mainBitmapIndex++] = newBitmap[newBitmapIndex++];
					mainBitmap[mainBitmapIndex++] = newBitmap[newBitmapIndex++];
					
					//mainBitmap[mainBitmapIndex++]^= newBitmap[newBitmapIndex++];
					//mainBitmap[mainBitmapIndex++]^= newBitmap[newBitmapIndex++];
					//mainBitmap[mainBitmapIndex++]^= newBitmap[newBitmapIndex++];
					//mainBitmap[mainBitmapIndex++]^= newBitmap[newBitmapIndex++];
				}
			}
		}

		public void run() {

			while (!Thread.currentThread().isInterrupted()) {

				try {
					String packetResult = "Empty";
					// look for header
					
					if((dis.read()==1)&&(dis.read()==2)&&(dis.read()==3)&&(dis.read()==4)) {
						int lengthOfPacket = dis.readInt();
						int xOffset = dis.readInt();
						int yOffset = dis.readInt();
						mouseX = dis.readInt();
						mouseY = dis.readInt();
						Log.d("JHHMSG","header received");
						packetResult = "Header received, reading "+lengthOfPacket+" bytes";
						// we have found a packet header, we need to read the next lengthOfPacket bytes
						dis.readFully(inputBuf,0,lengthOfPacket);
						Log.d("JHHMSG","finished reading packet of "+lengthOfPacket+" bytes");
						packetResult = "Header received, reading "+lengthOfPacket+" bytes. finshed read";
						int length  = QuickLZ.decompress(inputBuf,uncompressedBuf);
						Log.d("JHHMSG","uncompressed to  "+length+" bytes");
						
						int bmpHeaderLength = readIntFromArrayLSB(uncompressedBuf,10+CASTER_HEADER_LENGTH);
						if(currentImageBufLength == 0) {
							currentImageBufLength = length;
							
						}
						copyNewBitmapBufferIntoMainBitmap(currentImageBuf,uncompressedBuf,xOffset,yOffset,length);
						
						
						
						try {
							Bitmap newbmp = BitmapFactory.decodeByteArray(currentImageBuf, 0, currentImageBufLength);
							if(newbmp!=null) {
								if(clientBitmap!=null) {
									clientBitmap.recycle();
									
								}
								clientBitmap = newbmp;
								v.postInvalidate();
							}
							
						} catch (Exception e) {
							Log.d("JHHMSG","EXCEPTION thrown in decodeByteArray");
							
								
						}
						
						Log.d("JHHMSG","finished creating bitmap");
						
						
					}
					
					//updateConversationHandler.post(new updateUIThread(packetResult));

				} catch (IOException e) {
					e.printStackTrace();
					Log.d("JHHMSG","exception in client socket loop");
					//return;
				}
			}
		}

	}

	

    
    
    

}