/*
    ......... 2015 Ivan Mahonin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#ifndef _VECTOR_H_
#define _VECTOR_H_

#include <cmath>

class Vector {
public:
	typedef float float;

private:
	float _x, _y;

public:
	Vector(): _x(0.0), _y(0.0) { };
	Vector(const float &x, const float &y):_x(x),_y(y) { };
	Vector(const float &radius, const Angle &angle):
	_x(radius*Angle::cos(angle).get()),
	_y(radius*Angle::sin(angle).get())
	{ };

	bool is_valid()const { return !(isnan(_x) || isnan(_y)); }
	bool is_nan_or_inf()const { return isnan(_x) || isnan(_y) || isinf(_x) || isinf(_y); }

	float &
	operator[](const int& i)
	{ return i?_y:_x; }

	const float &
	operator[](const int& i) const
	{ return i?_y:_x; }

	const Vector &
	operator+=(const Vector &rhs)
	{
		_x+=rhs._x;
		_y+=rhs._y;
		return *this;
	}

	const Vector &
	operator-=(const Vector &rhs)
	{
		_x-=rhs._x;
		_y-=rhs._y;
		return *this;
	}

	const Vector &
	operator*=(const float &rhs)
	{
		_x*=rhs;
		_y*=rhs;
		return *this;
	}

	const Vector &
	operator/=(const float &rhs)
	{
		float tmp=1.0/rhs;
		_x*=tmp;
		_y*=tmp;
		return *this;
	}

	Vector
	operator+(const Vector &rhs)const
		{ return Vector(*this)+=rhs; }

	Vector
	operator-(const Vector &rhs)const
		{ return Vector(*this)-=rhs; }

	Vector
	operator*(const float &rhs)const
		{ return Vector(*this)*=rhs; }

	Vector
	operator/(const float &rhs)const
		{ return Vector(*this)/=rhs; }

	Vector
	operator-()const
		{ return Vector(-_x,-_y); }

	float
	operator*(const Vector &rhs)const
		{ return _x*rhs._x+_y*rhs._y; }

	bool
	operator==(const Vector &rhs)const
		{ return _x==rhs._x && _y==rhs._y; }

	bool
	operator!=(const Vector &rhs)const
		{ return _y!=rhs._y || _x!=rhs._x; }

	//! Returns the squared magnitude of the vector
	float mag_squared()const
		{ return _x*_x+_y*_y; }

	//! Returns the magnitude of the vector
	float mag()const
		{ return sqrt(mag_squared()); }

	//! Returns the reciprocal of the magnitude of the vector
	float inv_mag()const
		{ return 1.0/sqrt(mag_squared()); }

	//! Returns a normalized version of the vector
	Vector norm()const
		{ return (*this)*inv_mag(); }

	//! Returns a perpendicular version of the vector
	Vector perp()const
		{ return Vector(_y,-_x); }

	Angle angle()const
		{ return Angle::rad(atan2(_y, _x)); }

	bool is_equal_to(const Vector& rhs)const
	{
		static const float epsilon(0.0000000000001);
//		return (_x>rhs._x)?_x-rhs._x<=epsilon:rhs._x-_x<=epsilon && (_y>rhs._y)?_y-rhs._y<=epsilon:rhs._y-_y<=epsilon;
		return (*this-rhs).mag_squared()<=epsilon;
	}

	static const Vector zero() { return Vector(0,0); }

	Vector multiply_coords(const Vector &rhs) const
		{ return Vector(_x*rhs._x, _y*rhs._y); }
	Vector divide_coords(const Vector &rhs) const
		{ return Vector(_x/rhs._x, _y/rhs._y); }
	Vector one_divide_coords() const
		{ return Vector(1.0/_x, 1.0/_y); }
	Vector rotate(const Angle &rhs) const
	{
		float s = Angle::sin(rhs).get();
		float c = Angle::cos(rhs).get();
		return Vector(c*_x - s*_y, s*_x + c*_y);
	}
};

/*!	\typedef Point
**	\todo writeme
*/
typedef Vector Point;



}; // END of namespace synfig

namespace std {

inline synfig::Vector::float
abs(const synfig::Vector &rhs)
	{ return rhs.mag(); }

}; // END of namespace std

#include <ETL/bezier>

_ETL_BEGIN_NAMESPACE

template <>
class bezier_base<synfig::Vector,float> : public std::unary_function<float,synfig::Vector>
{
public:
	typedef synfig::Vector float;
	typedef float time_type;
private:

	bezier_base<synfig::Vector::float,time_type> bezier_x,bezier_y;

	float a,b,c,d;

protected:
	affine_combo<float,time_type> affine_func;

public:
	bezier_base() { }
	bezier_base(
		const float &a, const float &b, const float &c, const float &d,
		const time_type &r=0.0, const time_type &s=1.0):
		a(a),b(b),c(c),d(d) { set_rs(r,s); sync(); }

	void sync()
	{
		bezier_x[0]=a[0],bezier_y[0]=a[1];
		bezier_x[1]=b[0],bezier_y[1]=b[1];
		bezier_x[2]=c[0],bezier_y[2]=c[1];
		bezier_x[3]=d[0],bezier_y[3]=d[1];
		bezier_x.sync();
		bezier_y.sync();
	}

	float
	operator()(time_type t)const
	{
		return synfig::Vector(bezier_x(t),bezier_y(t));
	}

	void evaluate(time_type t, float &f, float &df) const
	{
		t=(t-get_r())/get_dt();

		const float p1 = affine_func(
							affine_func(a,b,t),
							affine_func(b,c,t)
							,t);
		const float p2 = affine_func(
							affine_func(b,c,t),
							affine_func(c,d,t)
						,t);

		f = affine_func(p1,p2,t);
		df = (p2-p1)*3;
	}

	void set_rs(time_type new_r, time_type new_s) { bezier_x.set_rs(new_r,new_s); bezier_y.set_rs(new_r,new_s); }
	void set_r(time_type new_r) { bezier_x.set_r(new_r); bezier_y.set_r(new_r); }
	void set_s(time_type new_s) { bezier_x.set_s(new_s); bezier_y.set_s(new_s); }
	const time_type &get_r()const { return bezier_x.get_r(); }
	const time_type &get_s()const { return bezier_x.get_s(); }
	time_type get_dt()const { return bezier_x.get_dt(); }

	float &
	operator[](int i)
	{ return (&a)[i]; }

	const float &
	operator[](int i) const
	{ return (&a)[i]; }

	//! Bezier curve intersection function
	time_type intersect(const bezier_base<float,time_type> &/*x*/, time_type /*near*/=0.0)const
	{
		return 0;
	}
};

#endif
