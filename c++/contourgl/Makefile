DEPLIBS = gl x11 OpenCL

CXXFLAGS = -O0 -g -Wall -fmessage-length=0 `pkg-config --cflags $(DEPLIBS)` -DGL_GLEXT_PROTOTYPES

HEADERS = \
	clcontext.h \
	contour.h \
	contourbuilder.h \
	environment.h \
	geometry.h \
	glcontext.h \
	measure.h \
	polyspan.h \
	rendersw.h \
	shaders.h \
	test.h \
	triangulator.h \
	utils.h

SOURCES = \
	contourgl.cpp \
	clcontext.cpp \
	contour.cpp \
	contourbuilder.cpp \
	environment.cpp \
	geometry.cpp \
	glcontext.cpp \
	measure.cpp \
	polyspan.cpp \
	rendersw.cpp \
	shaders.cpp \
	test.cpp \
	triangulator.cpp \
	utils.cpp

OBJS = \
	contourgl.o \
	clcontext.o \
	contour.o \
	contourbuilder.o \
	environment.o \
	geometry.o \
	glcontext.o \
	measure.o \
	polyspan.o \
	rendersw.o \
	shaders.o \
	test.o \
	triangulator.o \
	utils.o

DEPS = $(HEADERS) $(SOURCES) 
LIBS = `pkg-config --libs $(DEPLIBS)`
TARGET = contourgl

$(TARGET):	$(OBJS)
	$(CXX) -o $(TARGET) $(OBJS) $(LIBS)

all:	$(TARGET)

clean:
	rm -f $(OBJS) $(TARGET)
