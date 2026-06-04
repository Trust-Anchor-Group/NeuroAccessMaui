package com.neuroaccess.nfc.testfixtures;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.math.BigInteger;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.KeyPair;
import java.security.KeyPairGenerator;
import java.security.MessageDigest;
import java.security.PrivateKey;
import java.security.SecureRandom;
import java.security.Security;
import java.security.cert.X509Certificate;
import java.security.spec.ECGenParameterSpec;
import java.security.spec.MGF1ParameterSpec;
import java.security.spec.PSSParameterSpec;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.bouncycastle.asn1.x9.ECNamedCurveTable;
import org.bouncycastle.asn1.x9.X9ECParameters;
import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.cert.X509v3CertificateBuilder;
import org.bouncycastle.cert.jcajce.JcaX509CertificateConverter;
import org.bouncycastle.cert.jcajce.JcaX509v3CertificateBuilder;
import org.bouncycastle.jce.spec.ECParameterSpec;
import org.bouncycastle.jce.provider.BouncyCastleProvider;
import org.bouncycastle.operator.ContentSigner;
import org.bouncycastle.operator.jcajce.JcaContentSignerBuilder;
import org.jmrtd.lds.LDSFile;
import org.jmrtd.lds.SODFile;
import org.jmrtd.lds.DisplayedImageInfo;
import org.jmrtd.lds.ImageInfo;
import org.jmrtd.lds.icao.COMFile;
import org.jmrtd.lds.icao.DG1File;
import org.jmrtd.lds.icao.DG11File;
import org.jmrtd.lds.icao.DG12File;
import org.jmrtd.lds.icao.DG14File;
import org.jmrtd.lds.icao.DG15File;
import org.jmrtd.lds.icao.DG2File;
import org.jmrtd.lds.icao.DG3File;
import org.jmrtd.lds.icao.DG4File;
import org.jmrtd.lds.icao.DG5File;
import org.jmrtd.lds.icao.DG7File;
import org.jmrtd.lds.icao.MRZInfo;
import org.jmrtd.lds.iso19794.FaceImageInfo;
import org.jmrtd.lds.iso19794.FaceInfo;
import org.jmrtd.lds.iso19794.FingerImageInfo;
import org.jmrtd.lds.iso19794.FingerInfo;
import org.jmrtd.lds.iso19794.IrisBiometricSubtypeInfo;
import org.jmrtd.lds.iso19794.IrisImageInfo;
import org.jmrtd.lds.iso19794.IrisInfo;
import org.jmrtd.lds.iso39794.FaceImageDataBlock;
import org.jmrtd.lds.iso39794.FaceImageInformation2DBlock;
import org.jmrtd.lds.iso39794.FaceImageInformation2DBlock.FaceImageKind2DCode;
import org.jmrtd.lds.iso39794.FaceImageInformation2DBlock.ImageColourSpaceCode;
import org.jmrtd.lds.iso39794.FaceImageInformation2DBlock.ImageDataFormatCode;
import org.jmrtd.lds.iso39794.FaceImageInformation2DBlock.ImageSizeBlock;
import org.jmrtd.lds.iso39794.FaceImageInformation2DBlock.LossyTransformationAttemptsCode;
import org.jmrtd.lds.iso39794.FaceImageRepresentation2DBlock;
import org.jmrtd.lds.iso39794.FaceImageRepresentationBlock;
import org.jmrtd.lds.iso39794.VersionBlock;
import org.jmrtd.lds.iso19794.FaceImageInfo.EyeColor;

import net.sf.scuba.data.Gender;

/**
 * Generates synthetic LDS fixture files with JMRTD.
 */
public final class JmrtdFixtureGenerator {

  private static final String GENERATOR_VERSION = "1.0.0";
  private static final String JMRTD_VERSION = "0.8.6";
  private static final String LDS_VERSION = "1.8";
  private static final String UNICODE_VERSION = "6.0.0";
  private static final int DG1_TAG = 0x61;
  private static final int DG2_TAG = 0x75;
  private static final int DG3_TAG = 0x63;
  private static final int DG4_TAG = 0x76;
  private static final int DG5_TAG = 0x65;
  private static final int DG6_TAG = 0x66;
  private static final int DG7_TAG = 0x67;
  private static final int DG8_TAG = 0x68;
  private static final int DG9_TAG = 0x69;
  private static final int DG10_TAG = 0x6A;
  private static final int DG11_TAG = 0x6B;
  private static final int DG12_TAG = 0x6C;
  private static final int DG13_TAG = 0x6D;
  private static final int DG14_TAG = 0x6E;
  private static final int DG15_TAG = 0x6F;
  private static final int DG16_TAG = 0x70;

  private JmrtdFixtureGenerator() {
  }

  /**
   * Generates fixtures into the provided output directory.
   *
   * @param args first argument may be the output directory
   * @throws Exception if generation fails
   */
  public static void main(String[] args) throws Exception {
    Security.addProvider(new BouncyCastleProvider());

    Path outputDirectory = args.length > 0
        ? Path.of(args[0])
        : Path.of("..", "TestData", "JMRTD");
    Files.createDirectories(outputDirectory);

    KeyMaterial keyMaterial = createRsaKeyMaterial();
    KeyMaterial ecdsaExplicitKeyMaterial = createExplicitEcdsaKeyMaterial();

    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.minimal("minimal-iso19794-jpeg",
        "ISO19794", "JPEG", 320, 240, false, false, 0));
    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.minimal("dg11-iso19794-jpeg",
        "ISO19794", "JPEG", 320, 240, true, false, 0));
    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.minimal("minimal-iso19794-jpeg2000",
        "ISO19794", "JPEG2000", 320, 240, false, false, 0));
    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.minimal("large-iso19794-jpeg",
        "ISO19794", "JPEG", 640, 480, false, false, 90000));
    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.minimal("no-dg2-dg11",
        "NONE", "NONE", 0, 0, true, true, 0));
    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.minimal("minimal-iso39794-jpeg",
        "ISO39794", "JPEG", 320, 240, false, false, 0));
    writeFixture(outputDirectory, ecdsaExplicitKeyMaterial,
        FixtureDefinition.ecdsaSha512Explicit("minimal-ecdsa-sha512-explicit-iso19794-jpeg"));
    writeFixture(outputDirectory, createExplicitEcdsaKeyMaterial("brainpoolP512r1",
        "NeuroAccess JMRTD explicit Brainpool ECDSA fixture key v1",
        "CN=NeuroAccess Synthetic Brainpool ECDSA Explicit JMRTD DSC,C=SE",
        BigInteger.valueOf(3), "SHA512withECDSA"),
        FixtureDefinition.ecdsaSha512ExplicitBrainpool("minimal-brainpoolp512r1-ecdsa-sha512-explicit-iso19794-jpeg"));
    writeFixture(outputDirectory, keyMaterial,
        FixtureDefinition.rsaPssSha256("minimal-rsapss-sha256-iso19794-jpeg"));
    writeFixture(outputDirectory, createNamedEcdsaKeyMaterial("secp384r1",
        "NeuroAccess JMRTD named ECDSA fixture key v1",
        "CN=NeuroAccess Synthetic ECDSA Named JMRTD DSC,C=SE",
        BigInteger.valueOf(4), "SHA384withECDSA"),
        FixtureDefinition.ecdsaSha384Named("minimal-ecdsa-sha384-named-iso19794-jpeg"));
    writeFixture(outputDirectory, keyMaterial,
        FixtureDefinition.lds110("minimal-lds110-rsa-sha256-iso19794-jpeg"));
    writeFixture(outputDirectory, keyMaterial, FixtureDefinition.coverage("coverage-lds-all-files"));

    writeNotice(outputDirectory);
  }

  private static void writeFixture(Path outputDirectory, KeyMaterial keyMaterial,
      FixtureDefinition definition) throws Exception {
    Path fixtureDirectory = outputDirectory.resolve(definition.name);
    deleteDirectory(fixtureDirectory);
    Files.createDirectories(fixtureDirectory);

    MRZInfo mrzInfo = MRZInfo.createTD3MRZInfo("P", "UTO", "SPECIMEN", "ALICE",
        definition.documentNumber, "UTO", "900101", Gender.FEMALE, "350101", "");
    byte[] dg1 = encode(new DG1File(mrzInfo));

    LinkedHashMap<String, byte[]> files = new LinkedHashMap<String, byte[]>();
    LinkedHashMap<Integer, byte[]> dataGroupHashes = new LinkedHashMap<Integer, byte[]>();
    List<Integer> dataGroupTags = new ArrayList<Integer>();

    files.put("EF.DG1.bin", dg1);
    dataGroupHashes.put(1, hash(dg1, definition.digestAlgorithm));
    dataGroupTags.add(DG1_TAG);

    if (!definition.omitDg2) {
      byte[] dg2 = createDg2(definition);
      files.put("EF.DG2.bin", dg2);
      dataGroupHashes.put(2, hash(dg2, definition.digestAlgorithm));
      dataGroupTags.add(DG2_TAG);
    }

    if (definition.includeDg11) {
      byte[] dg11 = encode(new DG11File("SPECIMEN<<ALICE",
          Arrays.asList("SAMPLE<<ALICE"),
          "SYNTHETIC-0001",
          "19900101",
          Arrays.asList("STOCKHOLM"),
          Arrays.asList("TEST STREET 1"),
          null, "TESTER", null, null, null, null, null));
      files.put("EF.DG11.bin", dg11);
      dataGroupHashes.put(11, hash(dg11, definition.digestAlgorithm));
      dataGroupTags.add(DG11_TAG);
    }

    if (definition.includeCoverageFiles) {
      putDataGroup(files, dataGroupHashes, dataGroupTags, 3, DG3_TAG,
          "EF.DG3.bin", createDg3());
      putDataGroup(files, dataGroupHashes, dataGroupTags, 4, DG4_TAG,
          "EF.DG4.bin", createDg4());
      putDataGroup(files, dataGroupHashes, dataGroupTags, 5, DG5_TAG,
          "EF.DG5.bin", createDg5());
      putDataGroup(files, dataGroupHashes, dataGroupTags, 6, DG6_TAG,
          "EF.DG6.bin", createPresenceOnlyDataGroup(DG6_TAG));
      putDataGroup(files, dataGroupHashes, dataGroupTags, 7, DG7_TAG,
          "EF.DG7.bin", createDg7());
      putDataGroup(files, dataGroupHashes, dataGroupTags, 8, DG8_TAG,
          "EF.DG8.bin", createPresenceOnlyDataGroup(DG8_TAG));
      putDataGroup(files, dataGroupHashes, dataGroupTags, 9, DG9_TAG,
          "EF.DG9.bin", createPresenceOnlyDataGroup(DG9_TAG));
      putDataGroup(files, dataGroupHashes, dataGroupTags, 10, DG10_TAG,
          "EF.DG10.bin", createPresenceOnlyDataGroup(DG10_TAG));
      putDataGroup(files, dataGroupHashes, dataGroupTags, 12, DG12_TAG,
          "EF.DG12.bin", createDg12());
      putDataGroup(files, dataGroupHashes, dataGroupTags, 13, DG13_TAG,
          "EF.DG13.bin", createPresenceOnlyDataGroup(DG13_TAG));
      putDataGroup(files, dataGroupHashes, dataGroupTags, 14, DG14_TAG,
          "EF.DG14.bin", createDg14());
      putDataGroup(files, dataGroupHashes, dataGroupTags, 15, DG15_TAG,
          "EF.DG15.bin", createDg15(keyMaterial));
      putDataGroup(files, dataGroupHashes, dataGroupTags, 16, DG16_TAG,
          "EF.DG16.bin", createPresenceOnlyDataGroup(DG16_TAG));
      files.put("EF.DIR.bin", createPresenceOnlyDataGroup(0x61));
    }

    byte[] com = encode(new COMFile(definition.ldsVersion, UNICODE_VERSION,
        dataGroupTags.stream().mapToInt(Integer::intValue).toArray()));
    byte[] sod;
    if ("RSA-2048-PSS".equals(definition.signerKeyEncoding)) {
      PSSParameterSpec pssParameterSpec = new PSSParameterSpec("SHA-256", "MGF1",
          MGF1ParameterSpec.SHA256, 32, 1);
      sod = encode(new SODFile(definition.digestAlgorithm, definition.signatureAlgorithm,
          pssParameterSpec, dataGroupHashes, keyMaterial.privateKey, keyMaterial.certificate, "BC",
          definition.ldsVersion, UNICODE_VERSION));
    } else {
      sod = encode(new SODFile(definition.digestAlgorithm, definition.signatureAlgorithm,
          dataGroupHashes, keyMaterial.privateKey, keyMaterial.certificate, "BC",
          definition.ldsVersion, UNICODE_VERSION));
    }

    files.put("EF.COM.bin", com);
    files.put("EF.SOD.bin", sod);

    validateWithJmrtd(com, dg1, files.get("EF.DG2.bin"), files.get("EF.DG11.bin"), sod);

    for (Map.Entry<String, byte[]> entry : files.entrySet()) {
      Files.write(fixtureDirectory.resolve(entry.getKey()), entry.getValue());
    }

    Files.writeString(fixtureDirectory.resolve("manifest.json"),
        createManifest(definition, mrzInfo.toString(), files, dataGroupHashes),
        StandardCharsets.UTF_8);
  }

  private static byte[] createDg2(FixtureDefinition definition) throws IOException {
    byte[] image = switch (definition.imageType) {
      case "JPEG" -> createJpeg(definition.width, definition.height, definition.extraImageBytes);
      case "JPEG2000" -> createJp2(definition.width, definition.height);
      default -> throw new IllegalArgumentException("Unsupported image type " + definition.imageType);
    };

    if ("ISO39794".equals(definition.dg2Encoding)) {
      ImageDataFormatCode formatCode = "JPEG2000".equals(definition.imageType)
          ? ImageDataFormatCode.JPEG2000_LOSSY
          : ImageDataFormatCode.JPEG;
      FaceImageInformation2DBlock imageInformation = new FaceImageInformation2DBlock(
          formatCode,
          FaceImageKind2DCode.MRTD,
          null,
          LossyTransformationAttemptsCode.ZERO,
          null,
          null,
          null,
          new ImageSizeBlock(definition.width, definition.height),
          null,
          ImageColourSpaceCode.RGB_24BIT,
          null);
      FaceImageRepresentation2DBlock representation2D =
          new FaceImageRepresentation2DBlock(image, imageInformation, null);
      FaceImageRepresentationBlock representation = new FaceImageRepresentationBlock(
          BigInteger.ONE, representation2D, null, null, null, null, null, null, null, null);
      FaceImageDataBlock dataBlock = new FaceImageDataBlock(
          new VersionBlock(3, 2019), List.of(representation), null);
      return encode(DG2File.createISO39794DG2File(List.of(dataBlock)));
    }

    int imageDataType = "JPEG2000".equals(definition.imageType)
        ? FaceImageInfo.IMAGE_DATA_TYPE_JPEG2000
        : FaceImageInfo.IMAGE_DATA_TYPE_JPEG;
    FaceImageInfo faceImageInfo = new FaceImageInfo(
        Gender.FEMALE,
        EyeColor.BROWN,
        0,
        FaceImageInfo.HAIR_COLOR_BROWN,
        FaceImageInfo.EXPRESSION_UNSPECIFIED,
        new int[] { 0, 0, 0 },
        new int[] { 0, 0, 0 },
        FaceImageInfo.FACE_IMAGE_TYPE_FULL_FRONTAL,
        FaceImageInfo.IMAGE_COLOR_SPACE_RGB24,
        FaceImageInfo.SOURCE_TYPE_STATIC_PHOTO_UNKNOWN_SOURCE,
        0,
        0,
        new FaceImageInfo.FeaturePoint[0],
        definition.width,
        definition.height,
        new ByteArrayInputStream(image),
        image.length,
        imageDataType);
    FaceInfo faceInfo = new FaceInfo(List.of(faceImageInfo));
    return encode(DG2File.createISO19794DG2File(List.of(faceInfo)));
  }

  private static byte[] createDg3() throws IOException {
    byte[] image = createJpeg(96, 96, 0);
    FingerImageInfo fingerImageInfo = new FingerImageInfo(
        FingerImageInfo.POSITION_RIGHT_INDEX_FINGER,
        1,
        1,
        80,
        FingerImageInfo.IMPRESSION_TYPE_LIVE_SCAN_PLAIN,
        96,
        96,
        new ByteArrayInputStream(image),
        image.length,
        FingerInfo.COMPRESSION_JPEG);
    FingerInfo fingerInfo = new FingerInfo(
        0,
        31,
        FingerInfo.SCALE_UNITS_PPI,
        500,
        500,
        500,
        500,
        8,
        FingerInfo.COMPRESSION_JPEG,
        List.of(fingerImageInfo));
    return encode(DG3File.createISO19794DG3File(List.of(fingerInfo)));
  }

  private static byte[] createDg4() throws IOException {
    byte[] image = createJpeg(160, 120, 0);
    IrisImageInfo irisImageInfo = new IrisImageInfo(
        1,
        IrisImageInfo.IMAGE_QUAL_HIGH_LO,
        0,
        0,
        160,
        120,
        new ByteArrayInputStream(image),
        image.length,
        IrisInfo.IMAGEFORMAT_RGB_JPEG);
    IrisBiometricSubtypeInfo subtypeInfo = new IrisBiometricSubtypeInfo(
        IrisBiometricSubtypeInfo.EYE_RIGHT,
        IrisInfo.IMAGEFORMAT_RGB_JPEG,
        List.of(irisImageInfo));
    IrisInfo irisInfo = new IrisInfo(
        IrisInfo.CAPTURE_DEVICE_UNDEF,
        IrisInfo.ORIENTATION_UNDEF,
        IrisInfo.ORIENTATION_UNDEF,
        IrisInfo.SCAN_TYPE_PROGRESSIVE,
        IrisInfo.IROCC_UNDEF,
        IrisInfo.IROCC_UNDEF,
        IrisInfo.IRBNDY_UNDEF,
        0,
        IrisInfo.IMAGEFORMAT_RGB_JPEG,
        160,
        120,
        8,
        IrisInfo.TRANS_STD,
        new byte[16],
        List.of(subtypeInfo));
    return encode(DG4File.createISO19794DG4File(List.of(irisInfo)));
  }

  private static byte[] createDg5() throws IOException {
    return encode(new DG5File(List.of(new DisplayedImageInfo(
        ImageInfo.TYPE_PORTRAIT,
        createJpeg(96, 120, 0)))));
  }

  private static byte[] createDg7() throws IOException {
    return encode(new DG7File(List.of(new DisplayedImageInfo(
        ImageInfo.TYPE_SIGNATURE_OR_MARK,
        createJpeg(160, 48, 0)))));
  }

  private static byte[] createDg12() {
    return encode(new DG12File("NeuroAccess Synthetic Authority",
        "20240101",
        List.of("SPECIMEN<<ALICE"),
        "Synthetic fixture for ICAO Part 3 coverage",
        "NONE",
        null,
        null,
        "20240101000000",
        "NA-SYNTH-0001"));
  }

  private static byte[] createDg14() {
    return encode(new DG14File(List.of()));
  }

  private static byte[] createDg15(KeyMaterial keyMaterial) {
    return encode(new DG15File(keyMaterial.certificate.getPublicKey()));
  }

  private static byte[] createPresenceOnlyDataGroup(int tag) {
    return new byte[] { (byte)tag, 0x00 };
  }

  private static void putDataGroup(LinkedHashMap<String, byte[]> files,
      LinkedHashMap<Integer, byte[]> dataGroupHashes, List<Integer> dataGroupTags,
      int dataGroupNumber, int dataGroupTag, String fileName, byte[] data) throws Exception {
    files.put(fileName, data);
    dataGroupHashes.put(dataGroupNumber, sha256(data));
    dataGroupTags.add(dataGroupTag);
  }

  private static byte[] createJpeg(int width, int height, int extraBytes) throws IOException {
    byte[] header = new byte[] {
        (byte)0xFF, (byte)0xD8,
        (byte)0xFF, (byte)0xC0,
        0x00, 0x11,
        0x08,
        (byte)(height >> 8), (byte)height,
        (byte)(width >> 8), (byte)width,
        0x03,
        0x01, 0x11, 0x00,
        0x02, 0x11, 0x00,
        0x03, 0x11, 0x00 };
    byte[] image = Arrays.copyOf(header, header.length + extraBytes + 2);
    for (int i = header.length; i < header.length + extraBytes; i++) {
      image[i] = (byte)(i & 0xFF);
    }
    image[image.length - 2] = (byte)0xFF;
    image[image.length - 1] = (byte)0xD9;
    return image;
  }

  private static byte[] createJp2(int width, int height) throws IOException {
    byte[] signature = new byte[] {
        0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, (byte)0x87, 0x0A };
    byte[] ftyp = new byte[] {
        0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x6A, 0x70, 0x32, 0x20,
        0x00, 0x00, 0x00, 0x00, 0x6A, 0x70, 0x32, 0x20 };
    byte[] jp2h = new byte[] {
        0x00, 0x00, 0x00, 0x2D, 0x6A, 0x70, 0x32, 0x68,
        0x00, 0x00, 0x00, 0x16, 0x69, 0x68, 0x64, 0x72,
        (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
        (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
        0x00, 0x03, 0x07, 0x07, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x0F, 0x63, 0x6F, 0x6C, 0x72,
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10 };
    byte[] jp2c = new byte[] {
        0x00, 0x00, 0x00, 0x0C, 0x6A, 0x70, 0x32, 0x63, (byte)0xFF, 0x4F, (byte)0xFF, 0x51 };

    byte[] result = new byte[signature.length + ftyp.length + jp2h.length + jp2c.length];
    int offset = 0;
    System.arraycopy(signature, 0, result, offset, signature.length);
    offset += signature.length;
    System.arraycopy(ftyp, 0, result, offset, ftyp.length);
    offset += ftyp.length;
    System.arraycopy(jp2h, 0, result, offset, jp2h.length);
    offset += jp2h.length;
    System.arraycopy(jp2c, 0, result, offset, jp2c.length);
    return result;
  }

  private static byte[] encode(LDSFile file) {
    return file.getEncoded();
  }

  private static byte[] sha256(byte[] data) throws Exception {
    return hash(data, "SHA-256");
  }

  private static byte[] hash(byte[] data, String algorithm) throws Exception {
    MessageDigest digest = MessageDigest.getInstance(algorithm);
    return digest.digest(data);
  }

  private static KeyMaterial createRsaKeyMaterial() throws Exception {
    SecureRandom random = SecureRandom.getInstance("SHA1PRNG");
    random.setSeed("NeuroAccess JMRTD fixture key v1".getBytes(StandardCharsets.UTF_8));

    KeyPairGenerator keyPairGenerator = KeyPairGenerator.getInstance("RSA");
    keyPairGenerator.initialize(2048, random);
    KeyPair keyPair = keyPairGenerator.generateKeyPair();

    X500Name subject = new X500Name("CN=NeuroAccess Synthetic JMRTD DSC,C=SE");
    Date notBefore = Date.from(Instant.parse("2024-01-01T00:00:00Z"));
    Date notAfter = Date.from(Instant.parse("2034-01-01T00:00:00Z"));
    X509v3CertificateBuilder certificateBuilder = new JcaX509v3CertificateBuilder(
        subject, BigInteger.ONE, notBefore, notAfter, subject, keyPair.getPublic());
    ContentSigner signer = new JcaContentSignerBuilder("SHA256withRSA")
        .setProvider("BC")
        .build(keyPair.getPrivate());
    X509Certificate certificate = new JcaX509CertificateConverter()
        .setProvider("BC")
        .getCertificate(certificateBuilder.build(signer));

    return new KeyMaterial(keyPair.getPrivate(), certificate);
  }

  private static KeyMaterial createExplicitEcdsaKeyMaterial() throws Exception {
    return createExplicitEcdsaKeyMaterial("secp521r1",
        "NeuroAccess JMRTD explicit ECDSA fixture key v1",
        "CN=NeuroAccess Synthetic ECDSA Explicit JMRTD DSC,C=SE",
        BigInteger.valueOf(2), "SHA512withECDSA");
  }

  private static KeyMaterial createExplicitEcdsaKeyMaterial(String curveName, String seed,
      String distinguishedName, BigInteger serialNumber, String certificateSignatureAlgorithm) throws Exception {
    SecureRandom random = SecureRandom.getInstance("SHA1PRNG");
    random.setSeed(seed.getBytes(StandardCharsets.UTF_8));

    X9ECParameters curve = ECNamedCurveTable.getByName(curveName);
    ECParameterSpec explicitSpec = new ECParameterSpec(
        curve.getCurve(), curve.getG(), curve.getN(), curve.getH(), curve.getSeed());

    KeyPairGenerator keyPairGenerator = KeyPairGenerator.getInstance("ECDSA", "BC");
    keyPairGenerator.initialize(explicitSpec, random);
    KeyPair keyPair = keyPairGenerator.generateKeyPair();

    X500Name subject = new X500Name(distinguishedName);
    Date notBefore = Date.from(Instant.parse("2026-01-01T00:00:00Z"));
    Date notAfter = Date.from(Instant.parse("2036-01-01T00:00:00Z"));
    X509v3CertificateBuilder certificateBuilder = new JcaX509v3CertificateBuilder(
        subject, serialNumber, notBefore, notAfter, subject, keyPair.getPublic());
    ContentSigner signer = new JcaContentSignerBuilder(certificateSignatureAlgorithm)
        .setProvider("BC")
        .build(keyPair.getPrivate());
    X509Certificate certificate = new JcaX509CertificateConverter()
        .setProvider("BC")
        .getCertificate(certificateBuilder.build(signer));

    return new KeyMaterial(keyPair.getPrivate(), certificate);
  }

  private static KeyMaterial createNamedEcdsaKeyMaterial(String curveName, String seed,
      String distinguishedName, BigInteger serialNumber, String certificateSignatureAlgorithm) throws Exception {
    SecureRandom random = SecureRandom.getInstance("SHA1PRNG");
    random.setSeed(seed.getBytes(StandardCharsets.UTF_8));

    KeyPairGenerator keyPairGenerator = KeyPairGenerator.getInstance("EC", "BC");
    keyPairGenerator.initialize(new ECGenParameterSpec(curveName), random);
    KeyPair keyPair = keyPairGenerator.generateKeyPair();

    X500Name subject = new X500Name(distinguishedName);
    Date notBefore = Date.from(Instant.parse("2026-01-01T00:00:00Z"));
    Date notAfter = Date.from(Instant.parse("2036-01-01T00:00:00Z"));
    X509v3CertificateBuilder certificateBuilder = new JcaX509v3CertificateBuilder(
        subject, serialNumber, notBefore, notAfter, subject, keyPair.getPublic());
    ContentSigner signer = new JcaContentSignerBuilder(certificateSignatureAlgorithm)
        .setProvider("BC")
        .build(keyPair.getPrivate());
    X509Certificate certificate = new JcaX509CertificateConverter()
        .setProvider("BC")
        .getCertificate(certificateBuilder.build(signer));

    return new KeyMaterial(keyPair.getPrivate(), certificate);
  }

  private static String createManifest(FixtureDefinition definition, String mrz,
      LinkedHashMap<String, byte[]> files, LinkedHashMap<Integer, byte[]> dataGroupHashes) throws Exception {
    StringBuilder builder = new StringBuilder();
    builder.append("{\n");
    appendJson(builder, 1, "generator", "JmrtdFixtureGenerator");
    appendJson(builder, 1, "generatorVersion", GENERATOR_VERSION);
    appendJson(builder, 1, "generatorSourceSha256", getGeneratorSourceHash());
    appendJson(builder, 1, "jmrtdVersion", JMRTD_VERSION);
    appendJson(builder, 1, "synthetic", true);
    appendJson(builder, 1, "dg2Encoding", definition.dg2Encoding);
    appendJson(builder, 1, "imageDataType", definition.imageType);
    appendJson(builder, 1, "ldsVersion", definition.ldsVersion);
    appendJson(builder, 1, "sodDigestAlgorithm", definition.digestAlgorithm);
    appendJson(builder, 1, "sodSignatureAlgorithm", definition.signatureAlgorithm);
    appendJson(builder, 1, "signerKeyEncoding", definition.signerKeyEncoding);
    appendJson(builder, 1, "documentNumber", definition.documentNumber);
    appendJson(builder, 1, "primaryIdentifier", "SPECIMEN");
    appendJson(builder, 1, "secondaryIdentifier", "ALICE");
    appendJson(builder, 1, "birthDate", "1990-01-01");
    appendJson(builder, 1, "expiryDate", "2035-01-01");
    appendJson(builder, 1, "nationality", "UTO");
    appendJson(builder, 1, "gender", "F");
    appendJson(builder, 1, "expectedFaceWidth", definition.width);
    appendJson(builder, 1, "expectedFaceHeight", definition.height);
    appendJson(builder, 1, "mrz", mrz.replace("\n", "\\n"));
    appendArray(builder, 1, "expectedDataGroups", dataGroupHashes.keySet());
    appendStringArray(builder, 1, "presenceOnlySyntheticFiles", definition.includeCoverageFiles
        ? List.of("EF.DG6.bin", "EF.DG8.bin", "EF.DG9.bin", "EF.DG10.bin",
            "EF.DG13.bin", "EF.DG16.bin", "EF.DIR.bin")
        : List.of());
    builder.append("  \"files\": {\n");
    int fileIndex = 0;
    for (Map.Entry<String, byte[]> entry : files.entrySet()) {
      builder.append("    \"").append(entry.getKey()).append("\": {\n");
      builder.append("      \"length\": ").append(entry.getValue().length).append(",\n");
      builder.append("      \"sha256\": \"").append(hex(sha256(entry.getValue()))).append("\"\n");
      builder.append("    }");
      builder.append(++fileIndex == files.size() ? "\n" : ",\n");
    }
    builder.append("  },\n");
    builder.append("  \"sodDataGroupHashes\": {\n");
    int hashIndex = 0;
    for (Map.Entry<Integer, byte[]> entry : dataGroupHashes.entrySet()) {
      builder.append("    \"DG").append(entry.getKey()).append("\": \"").append(hex(entry.getValue())).append("\"");
      builder.append(++hashIndex == dataGroupHashes.size() ? "\n" : ",\n");
    }
    builder.append("  }\n");
    builder.append("}\n");
    return builder.toString();
  }

  private static void validateWithJmrtd(byte[] com, byte[] dg1, byte[] dg2, byte[] dg11, byte[] sod)
      throws IOException {
    new COMFile(new ByteArrayInputStream(com));
    new DG1File(new ByteArrayInputStream(dg1));
    if (dg2 != null) {
      new DG2File(new ByteArrayInputStream(dg2));
    }
    if (dg11 != null) {
      new DG11File(new ByteArrayInputStream(dg11));
    }
    new SODFile(new ByteArrayInputStream(sod));
  }

  private static String getGeneratorSourceHash() throws Exception {
    Path sourcePath = Path.of("src", "main", "java", "com", "neuroaccess", "nfc",
        "testfixtures", "JmrtdFixtureGenerator.java");
    if (!Files.exists(sourcePath)) {
      sourcePath = Path.of("NeuroAccess.Nfc.Test", "JmrtdFixtureGenerator", "src", "main",
          "java", "com", "neuroaccess", "nfc", "testfixtures", "JmrtdFixtureGenerator.java");
    }

    return Files.exists(sourcePath) ? hex(sha256(Files.readAllBytes(sourcePath))) : "unknown";
  }

  private static void appendJson(StringBuilder builder, int indent, String name, String value) {
    indent(builder, indent);
    builder.append("\"").append(name).append("\": \"").append(escape(value)).append("\",\n");
  }

  private static void appendJson(StringBuilder builder, int indent, String name, boolean value) {
    indent(builder, indent);
    builder.append("\"").append(name).append("\": ").append(value).append(",\n");
  }

  private static void appendJson(StringBuilder builder, int indent, String name, int value) {
    indent(builder, indent);
    builder.append("\"").append(name).append("\": ").append(value).append(",\n");
  }

  private static void appendArray(StringBuilder builder, int indent, String name, Iterable<Integer> values) {
    indent(builder, indent);
    builder.append("\"").append(name).append("\": [");
    int index = 0;
    for (Integer value : values) {
      if (index++ > 0) {
        builder.append(", ");
      }
      builder.append(value);
    }
    builder.append("],\n");
  }

  private static void appendStringArray(StringBuilder builder, int indent, String name, Iterable<String> values) {
    indent(builder, indent);
    builder.append("\"").append(name).append("\": [");
    int index = 0;
    for (String value : values) {
      if (index++ > 0) {
        builder.append(", ");
      }
      builder.append("\"").append(escape(value)).append("\"");
    }
    builder.append("],\n");
  }

  private static void indent(StringBuilder builder, int indent) {
    for (int i = 0; i < indent; i++) {
      builder.append("  ");
    }
  }

  private static String escape(String value) {
    return value.replace("\\", "\\\\").replace("\"", "\\\"");
  }

  private static String hex(byte[] data) {
    StringBuilder builder = new StringBuilder(data.length * 2);
    for (byte value : data) {
      builder.append(String.format("%02x", value & 0xFF));
    }
    return builder.toString();
  }

  private static void deleteDirectory(Path directory) throws IOException {
    if (!Files.exists(directory)) {
      return;
    }

    try (java.util.stream.Stream<Path> paths = Files.walk(directory)) {
      paths.sorted((left, right) -> right.compareTo(left))
          .forEach(path -> {
            try {
              Files.delete(path);
            } catch (IOException exception) {
              throw new RuntimeException(exception);
            }
          });
    }
  }

  private static void writeNotice(Path outputDirectory) throws IOException {
    String notice = """
        JMRTD Fixture Notice
        ====================

        The fixture files in this directory are synthetic test documents generated with JMRTD.
        They contain fake holder data only and must not be treated as real identity documents.

        Generator: NeuroAccess.Nfc.Test/JmrtdFixtureGenerator
        JMRTD version: 0.8.6
        JMRTD source: https://jmrtd.org/
        JMRTD license: GNU Lesser General Public License, version 3 or later.

        These generated LDS files are used as offline parser and APDU-download fixtures.
        JMRTD jars and third-party dependencies are intentionally not committed here.
        """;
    Files.writeString(outputDirectory.resolve("NOTICE.txt"), notice, StandardCharsets.UTF_8);
  }

  private record KeyMaterial(PrivateKey privateKey, X509Certificate certificate) {
  }

  private record FixtureDefinition(String name, String dg2Encoding, String imageType,
      int width, int height, boolean includeDg11, boolean omitDg2, boolean includeCoverageFiles, int extraImageBytes,
      String ldsVersion, String digestAlgorithm, String signatureAlgorithm, String signerKeyEncoding,
      String documentNumber) {
    private static FixtureDefinition minimal(String name, String dg2Encoding, String imageType,
        int width, int height, boolean includeDg11, boolean omitDg2, int extraImageBytes) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, dg2Encoding, imageType, width, height,
          includeDg11, omitDg2, false, extraImageBytes,
          LDS_VERSION, "SHA-256", "SHA256withRSA", "RSA-2048", documentNumber);
    }

    private static FixtureDefinition ecdsaSha512Explicit(String name) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, "ISO19794", "JPEG", 320, 240,
          false, false, false, 0,
          LDS_VERSION, "SHA-512", "SHA512withECDSA", "explicit-secp521r1", documentNumber);
    }

    private static FixtureDefinition ecdsaSha512ExplicitBrainpool(String name) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, "ISO19794", "JPEG", 320, 240,
          false, false, false, 0,
          LDS_VERSION, "SHA-512", "SHA512withECDSA", "explicit-brainpoolP512r1", documentNumber);
    }

    private static FixtureDefinition rsaPssSha256(String name) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, "ISO19794", "JPEG", 320, 240,
          false, false, false, 0,
          LDS_VERSION, "SHA-256", "RSASSA-PSS", "RSA-2048-PSS", documentNumber);
    }

    private static FixtureDefinition ecdsaSha384Named(String name) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, "ISO19794", "JPEG", 320, 240,
          false, false, false, 0,
          LDS_VERSION, "SHA-384", "SHA384withECDSA", "named-secp384r1", documentNumber);
    }

    private static FixtureDefinition lds110(String name) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, "ISO19794", "JPEG", 320, 240,
          false, false, false, 0,
          "1.10", "SHA-256", "SHA256withRSA", "RSA-2048", documentNumber);
    }

    private static FixtureDefinition coverage(String name) {
      String documentNumber = "NA" + Math.abs(name.hashCode() % 1000000);
      return new FixtureDefinition(name, "ISO19794", "JPEG", 320, 240,
          true, false, true, 0,
          LDS_VERSION, "SHA-256", "SHA256withRSA", "RSA-2048", documentNumber);
    }
  }
}
