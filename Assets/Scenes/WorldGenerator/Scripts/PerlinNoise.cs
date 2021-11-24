using System;
using System.Collections.Generic;
public class WorldGenerator
{
	int height;
	int width;
	public WorldGenerator(int nWidth, int nHeight) //Constructor
    {
		height = nHeight;
		width = nWidth;
    }

	static System.Random rnd = new System.Random();

	public int[] hashTable = new int[256 * 2]; //Random set of all integer values between 0-255

	public void PerlinRandomise() //randomly places all values 0-255 in the hashtable
	{
		//shuffle all values from 0-255

		for (int i = 0; i < 256; i++) //initialise values
		{
			hashTable[i] = i;
		}

		//Knuth shuffle

		for (int i = 0; i < 256; i++)
		{
			int swapvalues = rnd.Next(0, 255); //Get next random index to swap at
			int carriedValue = hashTable[swapvalues];
			hashTable[swapvalues] = hashTable[i];
			hashTable[i] = carriedValue;
			hashTable[i + 256] = carriedValue; //set buffer at the end to stop overflows
		}

	}

	public void GenerateNoise2D(double frequency, float borderOffsetX, float borderOffsetY, float borderOffsetPower, bool fractal, ref int[,] table) //frequency is the amount of islands. BorderOffset is the offset from the boundaries of the map as a decimal
	{
		//default max height is 255.
		//offset should decrease this "maximum value" by the distance from the offset
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{

				double dx = (double)x / height;
				double dy = (double)y / width;
				double noise;

				if (fractal)
				{
					noise = PerlinOctave(dx, dy, 8, 0.5, frequency); //uses octaves + perlin noise to generate from 0 to 1
				}
				else
				{
					noise = NoiseFunction((dx * frequency), (dy * frequency));
					noise = (noise + 1) / 2;
				}

				int elevation = (int)(noise * (ReturnBoundaryMaxHeight(x, y, borderOffsetX, borderOffsetY, borderOffsetPower))); //convert 0-1 to 0-255 (the depth value for later)
				table[x, y] = elevation;
			}
		}

	}

	public int ReturnBoundaryMaxHeight(int x, int y, float borderOffsetX, float borderOffsetY, float borderOffsetPower) //lowers the height mapping dependent on distance to the sides
	{
		//offset power is 0-1 for how much the border smoothing is applied. 
		int maxHeight = 255;

		float xOutside = -1; //0 to 1 of how much x is inside the border offset zone
		float yOutside = -1; //0 to 1 of how much y is inside the border offset zone

		if ((float)width * borderOffsetX > x)
		{
			float outVal = (width * borderOffsetX) - x;
			xOutside = outVal / (width * borderOffsetX);
		}
		else if ((float)width * (1 - borderOffsetX) < x)
		{
			float outVal = x - (width * (1 - borderOffsetX));
			xOutside = outVal / (width * borderOffsetX);
		}

		if ((float)height * borderOffsetY > y)
		{
			float outVal = (height * borderOffsetY) - y;
			yOutside = outVal / (height * borderOffsetY);
		}
		else if ((float)height * (1 - borderOffsetY) < y)
		{
			float outVal = y - (height * (1 - borderOffsetY));
			yOutside = outVal / (height * borderOffsetY);
		}

		if (xOutside == -1 && yOutside == -1) //When inside the map boundaries
		{
			return maxHeight;
		}
		else //When outside the map boundaries
		{
			float heightMultiplier = (xOutside > yOutside ? 1 - (xOutside * borderOffsetPower) : 1 - (yOutside * borderOffsetPower)); //from 0 to 1
			return (int)(maxHeight * heightMultiplier);
		}

	}
	public double PerlinOctave(double x, double y, int octaves, double persistence, double frequency)
	{
		double total = 0;
		double amplitude = 1;
		double maxValue = 0;

		for (int i = 0; i < octaves; i++)
		{
			total += NoiseFunction(x * frequency, y * frequency) * amplitude; //Use noisefunction * the value of the amplitude to get a new total

			maxValue += amplitude;
			amplitude *= persistence;
			frequency *= 2;
		}

		return (((total / maxValue) + 1) / 2); //Divide by the maxValue and then limit to 0 to 1
	}

	private double NoiseFunction(double x, double y)
	{
		int xi = (int)Math.Floor(x) % 255;
		int yi = (int)Math.Floor(y) % 255;
		//Get random values from 0-255 with consistent values in each direction
		int g1 = hashTable[hashTable[xi] + yi];
		int g2 = hashTable[hashTable[xi + 1] + yi];
		int g3 = hashTable[hashTable[xi] + yi + 1];
		int g4 = hashTable[hashTable[xi + 1] + yi + 1];

		//Get the relative value of the x and y
		double xf = x - Math.Floor(x);
		double yf = y - Math.Floor(y);

		//Get the gradient of the relative x y against the gradient of each point
		double d1 = Gradient(g1, xf, yf);
		double d2 = Gradient(g2, xf - 1, yf);
		double d3 = Gradient(g3, xf, yf - 1);
		double d4 = Gradient(g4, xf - 1, yf - 1);

		//improves noise quality by applying a fade effect
		double u = Fader(xf);
		double v = Fader(yf);

		//Interpolate on x twice and then on y to get a value from -1 to 1
		double x1Inter = Lerp(u, d1, d2);
		double x2Inter = Lerp(u, d3, d4);
		double depthValue = Lerp(v, x1Inter, x2Inter);

		return depthValue; //return the interpolated value as a number from -1 to 1
	}
	private double Lerp(double amount, double left, double right) //Linear interpolation
	{
		return ((1 - amount) * left + amount * right);
	}

	private double Fader(double rel) //Fades the value using the perlin fade formula 6t5-15t4+10t3
	{
		return rel * rel * rel * (rel * (rel * 6 - 15) + 10);
	}

	private double Gradient(int hash, double x, double y) //gets a random gradient from a set
	{
		switch (hash % 8)
		{
			case 0: return x + y; //Up
			case 1: return -x + y; //Left
			case 2: return x - y; //Down
			case 3: return -x - y; //Right 
			case 4: return Math.Sqrt(2) * x; //Top Right
			case 5: return Math.Sqrt(2) * y; //Bottom Right
			case 6: return Math.Sqrt(2) * -x; //Top Left
			case 7: return Math.Sqrt(2) * -y; //Bottom Left
			default: return 0;
		}
	}

    public int[,] Generate(double frequency, float borderOffsetX, float borderOffsetY, float borderOffsetPower, bool fractal)
    {
        int[,] heightMap = new int[width + 1, height + 1];
        PerlinRandomise(); //swap values in the buffer
        GenerateNoise2D(frequency, borderOffsetX, borderOffsetY, borderOffsetPower, true, ref heightMap); //Generate 2D noise (x,y,depth). Value provided is the frequency of noise, then the offset from the borders of the map
		return heightMap;
    }

}