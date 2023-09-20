using System.Text;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Serialization;

namespace NeuroAccessMaui.Services;

/// <summary>
/// Exports database contents to XML.
/// </summary>
public class XmlDatabaseExport : IDatabaseExport, IDisposable
{
	private readonly XmlWriter output;
	private readonly int binaryDataSizeLimit;
	private bool disposeWriter;

	/// <summary>
	/// Exports database contents to XML.
	/// </summary>
	/// <param name="Output">XML output is directed to this XML writer.</param>
	/// <param name="Indent">If output should be indented (true), or written without unnecessary whitespace (false).</param>
	/// <param name="BinaryDataSizeLimit">Size limit of binary data fields. If larger, only a byte count will be presented.</param>
	public XmlDatabaseExport(StringBuilder Output, bool Indent, int BinaryDataSizeLimit)
		: this(XmlWriter.Create(Output, XML.WriterSettings(Indent, true)), BinaryDataSizeLimit)
	{
		this.disposeWriter = true;
	}

	/// <summary>
	/// Exports database contents to XML.
	/// </summary>
	/// <param name="Output">XML output is directed to this XML writer.</param>
	/// <param name="BinaryDataSizeLimit">Size limit of binary data fields. If larger, only a byte count will be presented.</param>
	public XmlDatabaseExport(XmlWriter Output, int BinaryDataSizeLimit)
	{
		this.output = Output;
		this.binaryDataSizeLimit = BinaryDataSizeLimit;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (this.disposeWriter)
		{
			this.output.Flush();
			this.output.Dispose();
			this.disposeWriter = false;
		}
	}

	/// <inheritdoc/>
	public Task StartDatabase()
	{
		this.output.WriteStartElement("Database");
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task EndDatabase()
	{
		this.output.WriteEndElement();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task StartCollection(string CollectionName)
	{
		this.output.WriteStartElement("Collection");
		this.output.WriteAttributeString("name", CollectionName);
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task EndCollection()
	{
		this.output.WriteEndElement();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task StartIndex()
	{
		this.output.WriteStartElement("Index");
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task ReportIndexField(string FieldName, bool Ascending)
	{
		this.output.WriteStartElement("Index");
		this.output.WriteAttributeString("field", FieldName);
		this.output.WriteAttributeString("asc", CommonTypes.Encode(Ascending));
		this.output.WriteEndElement();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task EndIndex()
	{
		this.output.WriteEndElement();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task<string> StartObject(string ObjectId, string TypeName)
	{
		this.output.WriteStartElement("Obj");
		this.output.WriteAttributeString("id", ObjectId);
		this.output.WriteAttributeString("type", TypeName);
		return Task.FromResult<string>(ObjectId);
	}

	/// <inheritdoc/>
	public async Task ReportProperty(string PropertyName, object PropertyValue)
	{
		if (PropertyValue is null)
		{
			this.output.WriteStartElement("Null");
			if (PropertyName is not null)
				this.output.WriteAttributeString("n", PropertyName);
			this.output.WriteEndElement();
		}
		else if (PropertyValue is Enum)
		{
			this.output.WriteStartElement("En");
			if (PropertyName is not null)
				this.output.WriteAttributeString("n", PropertyName);
			this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
			this.output.WriteEndElement();
		}
		else
		{
			switch (Type.GetTypeCode(PropertyValue.GetType()))
			{
				case TypeCode.Boolean:
					this.output.WriteStartElement("Bl");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, CommonTypes.Encode((bool)PropertyValue));
					this.output.WriteEndElement();
					break;

				case TypeCode.Byte:
					this.output.WriteStartElement("B");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.Char:
					this.output.WriteStartElement("Ch");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.DateTime:
					this.output.WriteStartElement("DT");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, XML.Encode((DateTime)PropertyValue));
					this.output.WriteEndElement();
					break;

				case TypeCode.Decimal:
					this.output.WriteStartElement("Dc");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, CommonTypes.Encode((decimal)PropertyValue));
					this.output.WriteEndElement();
					break;

				case TypeCode.Double:
					this.output.WriteStartElement("Db");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, CommonTypes.Encode((double)PropertyValue));
					this.output.WriteEndElement();
					break;

				case TypeCode.Int16:
					this.output.WriteStartElement("I2");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.Int32:
					this.output.WriteStartElement("I4");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.Int64:
					this.output.WriteStartElement("I8");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.SByte:
					this.output.WriteStartElement("I1");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.Single:
					this.output.WriteStartElement("Fl");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, CommonTypes.Encode((float)PropertyValue));
					this.output.WriteEndElement();
					break;

				case TypeCode.String:
					string s = PropertyValue.ToString();
					try
					{
						XmlConvert.VerifyXmlChars(s);
						this.output.WriteStartElement("S");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("v", string.Empty, s);
						this.output.WriteEndElement();
					}
					catch (XmlException)
					{
						byte[] Bin = Encoding.UTF8.GetBytes(s);
						s = Convert.ToBase64String(Bin);
						this.output.WriteStartElement("S64");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("v", string.Empty, s);
						this.output.WriteEndElement();
					}
					break;

				case TypeCode.UInt16:
					this.output.WriteStartElement("U2");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.UInt32:
					this.output.WriteStartElement("U4");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.UInt64:
					this.output.WriteStartElement("U8");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
					this.output.WriteEndElement();
					break;

				case TypeCode.DBNull:
				case TypeCode.Empty:
					this.output.WriteStartElement("Null");
					if (PropertyName is not null)
						this.output.WriteAttributeString("n", PropertyName);
					this.output.WriteEndElement();
					break;

				case TypeCode.Object:
					if (PropertyValue is TimeSpan)
					{
						this.output.WriteStartElement("TS");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
						this.output.WriteEndElement();
					}
					else if (PropertyValue is DateTimeOffset DTO)
					{
						this.output.WriteStartElement("DTO");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("v", string.Empty, XML.Encode(DTO));
						this.output.WriteEndElement();
					}
					else if (PropertyValue is CaseInsensitiveString Cis)
					{
						s = Cis.Value;
						try
						{
							XmlConvert.VerifyXmlChars(s);
							this.output.WriteStartElement("CIS");
							if (PropertyName is not null)
								this.output.WriteAttributeString("n", PropertyName);
							this.output.WriteAttributeString("v", string.Empty, s);
							this.output.WriteEndElement();
						}
						catch (XmlException)
						{
							byte[] Bin = Encoding.UTF8.GetBytes(s);
							s = Convert.ToBase64String(Bin);
							this.output.WriteStartElement("CIS64");
							if (PropertyName is not null)
								this.output.WriteAttributeString("n", PropertyName);
							this.output.WriteAttributeString("v", string.Empty, s);
							this.output.WriteEndElement();
						}
					}
					else if (PropertyValue is byte[] Bin)
					{
						this.output.WriteStartElement("Bin");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);

						long c = Bin.Length;

						if (c <= this.binaryDataSizeLimit)
						{
							if (c <= 1024)
								this.output.WriteAttributeString("v", Convert.ToBase64String(Bin));
							else
							{
								byte[] Buf = null;
								long i = 0;
								long d;
								int j;

								while (i < c)
								{
									d = c - i;
									if (d > 49152)
										j = 49152;
									else
										j = (int)d;

									if (Buf is null)
									{
										if (i == 0 && j == c)
											Buf = Bin;
										else
											Buf = new byte[j];
									}

									if (Buf != Bin)
										Array.Copy(Bin, i, Buf, 0, j);

									this.output.WriteElementString("Chunk", Convert.ToBase64String(Buf, 0, j, Base64FormattingOptions.None));
									i += j;
								}
							}
						}
						else
							this.output.WriteAttributeString("bytes", c.ToString());

						this.output.WriteEndElement();
					}
					else if (PropertyValue is Guid)
					{
						this.output.WriteStartElement("ID");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("v", string.Empty, PropertyValue.ToString());
						this.output.WriteEndElement();
					}
					else if (PropertyValue is Array A)
					{
						this.output.WriteStartElement("Array");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("elementType", string.Empty, PropertyValue.GetType().GetElementType().FullName);

						foreach (object Obj in A)
							await this.ReportProperty(null, Obj);

						this.output.WriteEndElement();
					}
					else if (PropertyValue is GenericObject Obj)
					{
						this.output.WriteStartElement("Obj");
						if (PropertyName is not null)
							this.output.WriteAttributeString("n", PropertyName);
						this.output.WriteAttributeString("type", string.Empty, Obj.TypeName);

						foreach (KeyValuePair<string, object> P in Obj)
							await this.ReportProperty(P.Key, P.Value);

						this.output.WriteEndElement();
					}
					else
						throw new Exception("Unhandled property value type: " + PropertyValue.GetType().FullName);
					break;

				default:
					throw new Exception("Unhandled property value type: " + PropertyValue.GetType().FullName);
			}
		}
	}

	/// <inheritdoc/>
	public Task EndObject()
	{
		this.output.WriteEndElement();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task ReportError(string Message)
	{
		this.output.WriteElementString("Error", Message);
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public async Task ReportException(Exception Exception)
	{
		this.output.WriteStartElement("Exception");
		this.output.WriteAttributeString("message", Exception.Message);
		this.output.WriteElementString("StackTrace", Log.CleanStackTrace(Exception.StackTrace));

		if (Exception is AggregateException AggregateException)
		{
			foreach (Exception ex in AggregateException.InnerExceptions)
				await this.ReportException(ex);
		}
		else if (Exception.InnerException is not null)
			await this.ReportException(Exception.InnerException);

		this.output.WriteEndElement();
	}

}
