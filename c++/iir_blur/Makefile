DEPLIBS = 

CXXFLAGS = -O3 -g -Wall -fmessage-length=0 `pkg-config --cflags $(DEPLIBS)`

HEADERS = \
	surface.h

SOURCES = \
	iir_blur.cpp \
	surface.cpp

OBJS = \
	iir_blur.o \
	surface.o

DEPS = $(HEADERS) $(SOURCES) 
LIBS = `pkg-config --libs $(DEPLIBS)`
TARGET = iir_blur

$(TARGET):	$(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)
