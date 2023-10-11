namespace FatCat.Toolkit;

public interface IPhoneNumber
{
	bool Validate(string phoneNumber);
	
	
}

public class PhoneNumber : IPhoneNumber
{
	//Regex.Replace("1112224444", @"(\d{3})(\d{3})(\d{4})", "$1-$2-$3");
	
	public bool Validate(string phoneNumber) => throw new NotImplementedException();
}